using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace D2ApiCreator.Tests.Unit.Helpers;

/// <summary>Buduje skoroszyty XLSX w pamięci (tekst inline) do testów analizy szablonu arkusza.</summary>
public static class XlsxTestHelper
{
    /// <summary>Arkusz z wierszami; każdy wiersz to słownik kolumna („A”) → tekst. Pusty tekst pomija komórkę.</summary>
    public static MemoryStream CreateXlsx(string sheetName, params Dictionary<string, string>[] rows)
        => CreateXlsx(new[] { (sheetName, rows) });

    public static MemoryStream CreateXlsx(IEnumerable<(string Name, Dictionary<string, string>[] Rows)> sheets)
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheetList = workbookPart.Workbook.AppendChild(new Sheets());
            uint sheetId = 1;

            foreach (var (name, rows) in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                uint rowIndex = 1;
                foreach (var row in rows)
                {
                    var r = new Row { RowIndex = rowIndex };
                    foreach (var (column, text) in row.OrderBy(kv => kv.Key.Length).ThenBy(kv => kv.Key, StringComparer.Ordinal))
                    {
                        if (string.IsNullOrEmpty(text))
                        {
                            continue;
                        }
                        r.Append(new Cell
                        {
                            CellReference = $"{column}{rowIndex}",
                            DataType = CellValues.InlineString,
                            InlineString = new InlineString(new Text(text))
                        });
                    }
                    sheetData.Append(r);
                    rowIndex++;
                }
                worksheetPart.Worksheet = new Worksheet(sheetData);
                sheetList.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId++, Name = name });
            }
        }
        stream.Position = 0;
        return stream;
    }

    public static Dictionary<string, string> Row(params (string Column, string Text)[] cells)
        => cells.ToDictionary(c => c.Column, c => c.Text);
}
