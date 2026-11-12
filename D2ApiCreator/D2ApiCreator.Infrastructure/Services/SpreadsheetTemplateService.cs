using System.Text;
using System.Text.RegularExpressions;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Analiza szablonu XLSX (OpenXML SpreadsheetML). Reguły (2026-11-08, BR-021):
/// <list type="bullet">
/// <item><b>Wiersz powielany</b> — wszystkie niepuste komórki wiersza to pojedyncze znaczniki <c>&lt;%nazwa%&gt;</c>
///   (co najmniej dwie, albo jedna pod wierszem nagłówka). Kolumny mapują się na właściwości elementu listy
///   (nazwa znacznika bez wspólnego przedrostka), a przy jednej kolumnie — na element listy wartości prostych.</item>
/// <item><b>Nagłówek tabeli</b> — wiersz ze stałym tekstem (bez znaczników) bezpośrednio nad wierszem powielanym.</item>
/// <item><b>Luźny znacznik</b> — każdy inny znacznik („Koszt” | <c>&lt;%koszt%&gt;</c>): „Podstaw wartość” z etykietą
///   z najbliższej niepustej komórki po lewej (albo nad).</item>
/// </list>
/// Wiersze ze stałym tekstem bez wiersza powielanego pod spodem są zwykłą treścią arkusza.
/// </summary>
public class SpreadsheetTemplateService : ISpreadsheetTemplateService
{
    private static readonly Regex PlaceholderRegex = new(@"<%([a-zA-Z0-9_]+)%>", RegexOptions.Compiled);
    private static readonly Regex SinglePlaceholderRegex = new(@"^\s*<%([a-zA-Z0-9_]+)%>\s*$", RegexOptions.Compiled);
    private static readonly Regex CellRefRegex = new(@"^([A-Z]+)(\d+)$", RegexOptions.Compiled);

    private readonly ILogger<SpreadsheetTemplateService> _logger;

    public SpreadsheetTemplateService(ILogger<SpreadsheetTemplateService> logger)
    {
        _logger = logger;
    }

    private sealed record CellInfo(string Reference, string Column, int ColumnIndex, string Text)
    {
        public bool IsSinglePlaceholder => SinglePlaceholderRegex.IsMatch(Text);
        public bool HasPlaceholder => Text.Contains("<%", StringComparison.Ordinal);
    }

    private sealed record RowInfo(int Index, List<CellInfo> Cells)
    {
        public bool IsEmpty => Cells.Count == 0;
        public bool AllPlaceholders => Cells.Count > 0 && Cells.All(c => c.IsSinglePlaceholder);
        public bool AllConstant => Cells.Count > 0 && Cells.All(c => !c.HasPlaceholder);
    }

    public Task<SpreadsheetTemplateAnalysis> AnalyzeAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var analysis = new SpreadsheetTemplateAnalysis();
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var document = SpreadsheetDocument.Open(fileStream, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart?.Workbook?.Sheets == null)
        {
            _logger.LogWarning("Workbook {FileName} has no sheets", fileName);
            return Task.FromResult(analysis);
        }

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>()
            .Select(i => i.InnerText).ToList() ?? new List<string>();

        foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sheet.Id?.Value == null || workbookPart.GetPartById(sheet.Id.Value) is not WorksheetPart worksheetPart)
            {
                continue;
            }

            var sheetName = sheet.Name?.Value ?? "Arkusz";
            var rows = ReadRows(worksheetPart, sharedStrings);
            AnalyzeSheet(sheetName, rows, analysis, placeholders, tableNames);
        }

        analysis.Placeholders = placeholders.OrderBy(p => p, StringComparer.Ordinal).ToList();
        _logger.LogInformation("Spreadsheet {FileName}: {Placeholders} loose placeholders, {Tables} repeated rows",
            fileName, analysis.Placeholders.Count, analysis.Tables.Count);
        return Task.FromResult(analysis);
    }

    private static List<RowInfo> ReadRows(WorksheetPart worksheetPart, IReadOnlyList<string> sharedStrings)
    {
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        var rows = new List<RowInfo>();
        if (sheetData == null)
        {
            return rows;
        }

        foreach (var row in sheetData.Elements<Row>())
        {
            var cells = new List<CellInfo>();
            foreach (var cell in row.Elements<Cell>())
            {
                var text = CellText(cell, sharedStrings).Trim();
                if (text.Length == 0)
                {
                    continue;
                }
                var reference = cell.CellReference?.Value ?? string.Empty;
                var match = CellRefRegex.Match(reference);
                var column = match.Success ? match.Groups[1].Value : string.Empty;
                cells.Add(new CellInfo(reference, column, ColumnIndex(column), text));
            }
            var index = (int)(row.RowIndex?.Value ?? (uint)(rows.Count + 1));
            rows.Add(new RowInfo(index, cells.OrderBy(c => c.ColumnIndex).ToList()));
        }
        return rows;
    }

    private static string CellText(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            return int.TryParse(cell.CellValue?.Text, out var idx) && idx >= 0 && idx < sharedStrings.Count ? sharedStrings[idx] : string.Empty;
        }
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }
        return cell.CellValue?.Text ?? string.Empty;
    }

    private void AnalyzeSheet(string sheetName, List<RowInfo> rows, SpreadsheetTemplateAnalysis analysis,
        HashSet<string> placeholders, HashSet<string> tableNames)
    {
        var handledRows = new HashSet<int>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.IsEmpty)
            {
                continue;
            }

            var header = i > 0 && rows[i - 1].Index == row.Index - 1 && rows[i - 1].AllConstant ? rows[i - 1] : null;
            var isRepeatRow = row.AllPlaceholders && (row.Cells.Count >= 2 || header != null);
            if (isRepeatRow)
            {
                analysis.Tables.Add(BuildTable(sheetName, row, header, tableNames));
                handledRows.Add(row.Index);
                continue;
            }

            // Luźne znaczniki: każdy znacznik w komórce, etykieta z komórki po lewej (albo z wiersza wyżej w tej samej kolumnie)
            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cell = row.Cells[c];
                if (!cell.HasPlaceholder)
                {
                    continue;
                }
                var label = LabelFor(row, c, i > 0 ? rows[i - 1] : null);
                foreach (Match m in PlaceholderRegex.Matches(cell.Text))
                {
                    placeholders.Add(m.Value);
                    analysis.PlaceholderDetails.Add(new PlaceholderDetailDto
                    {
                        Placeholder = m.Value,
                        Label = label,
                        Sheet = sheetName,
                        Cell = cell.Reference
                    });
                }
            }
        }
    }

    private static string? LabelFor(RowInfo row, int cellPosition, RowInfo? previousRow)
    {
        for (var k = cellPosition - 1; k >= 0; k--)
        {
            if (!row.Cells[k].HasPlaceholder)
            {
                return row.Cells[k].Text;
            }
        }
        var column = row.Cells[cellPosition].Column;
        var above = previousRow?.Cells.FirstOrDefault(c => c.Column == column);
        return above != null && !above.HasPlaceholder ? above.Text : null;
    }

    private static ContentControlDetailDto BuildTable(string sheetName, RowInfo row, RowInfo? header, HashSet<string> tableNames)
    {
        var names = row.Cells.Select(c => SinglePlaceholderRegex.Match(c.Text).Groups[1].Value).ToList();
        var prefix = CommonPrefix(names);
        // Nazwa listy: wspólny przedrostek znaczników; przy jednej kolumnie — tekst nagłówka nad nią; inaczej arkusz + numer wiersza
        var singleHeader = row.Cells.Count == 1 ? header?.Cells.FirstOrDefault(h => h.Column == row.Cells[0].Column)?.Text : null;
        var baseName = !string.IsNullOrEmpty(prefix) ? prefix
            : !string.IsNullOrWhiteSpace(singleHeader) && Sanitize(singleHeader).Length > 0 ? Sanitize(singleHeader)
            : $"wiersz_{Sanitize(sheetName)}_{row.Index}";
        var name = baseName;
        var suffix = 2;
        while (!tableNames.Add(name))
        {
            name = $"{baseName}_{suffix++}";
        }

        var columns = row.Cells.Select((cell, idx) => new TemplateColumnDto
        {
            Cell = cell.Reference,
            Placeholder = cell.Text.Trim(),
            Property = row.Cells.Count == 1 ? null : PropertyName(names[idx], prefix),
            Header = header?.Cells.FirstOrDefault(h => h.Column == cell.Column)?.Text
        }).ToList();

        var headerCells = header?.Cells.Select(h => h.Text).ToList();
        var preview = new StringBuilder();
        if (headerCells != null)
        {
            preview.Append(string.Join(" | ", headerCells)).Append(" → ");
        }
        preview.Append(string.Join(" | ", row.Cells.Select(c => c.Text.Trim())));
        var previewText = preview.ToString();

        return new ContentControlDetailDto
        {
            Name = name,
            Kind = ContentControlKinds.Table,
            Location = sheetName,
            Level = ContentControlLevels.Row,
            Preview = previewText.Length <= 120 ? previewText : previewText[..119] + "…",
            ParagraphCount = 1,
            ContainsTable = true,
            ContainsList = false,
            IsRepeating = true,
            SuggestedWorkerType = "Powtórz blok",
            Sheet = sheetName,
            Range = $"{row.Cells.First().Reference}:{row.Cells.Last().Reference}",
            HeaderCells = headerCells,
            Columns = columns
        };
    }

    /// <summary>Wspólny przedrostek nazw znaczników do pierwszego „_” wyłącznie (<c>pozycje_nazwa</c>, <c>pozycje_kwota</c> → <c>pozycje</c>).</summary>
    public static string CommonPrefix(IReadOnlyList<string> names)
    {
        if (names.Count < 2)
        {
            return string.Empty;
        }
        var first = names[0].Split('_');
        var prefixParts = new List<string>();
        for (var i = 0; i < first.Length - 1; i++)
        {
            var candidate = string.Join('_', first.Take(i + 1));
            if (names.All(n => n.StartsWith(candidate + "_", StringComparison.Ordinal) && n.Length > candidate.Length + 1))
            {
                prefixParts.Add(first[i]);
            }
            else
            {
                break;
            }
        }
        return string.Join('_', prefixParts);
    }

    private static string PropertyName(string placeholderName, string prefix)
        => !string.IsNullOrEmpty(prefix) && placeholderName.StartsWith(prefix + "_", StringComparison.Ordinal)
            ? placeholderName[(prefix.Length + 1)..]
            : placeholderName;

    private const string PolishChars = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ";
    private const string LatinChars = "acelnoszzACELNOSZZ";

    /// <summary>Identyfikator do ścieżki w modelu danych: bez polskich znaków, małe litery, cyfry i „_” (jak <c>sourceIdentifier</c> w GUI).</summary>
    public static string Sanitize(string text)
    {
        var sb = new StringBuilder();
        var lastUnderscore = false;
        foreach (var raw in text)
        {
            var idx = PolishChars.IndexOf(raw);
            var ch = idx >= 0 ? LatinChars[idx] : raw;
            if (ch < 128 && char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastUnderscore = false;
            }
            else if (!lastUnderscore)
            {
                sb.Append('_');
                lastUnderscore = true;
            }
        }
        return sb.ToString().Trim('_');
    }

    private static int ColumnIndex(string column)
    {
        var index = 0;
        foreach (var ch in column)
        {
            index = index * 26 + (ch - 'A' + 1);
        }
        return index;
    }
}
