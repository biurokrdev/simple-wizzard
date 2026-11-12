using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Office2013.Word;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs;
using Microsoft.Extensions.Logging;
namespace D2ApiCreator.Infrastructure.Services;
/// <summary>
/// Service for extracting placeholders from DOCX documents
/// </summary>
public class DocumentPlaceholderService : IDocumentPlaceholderService
{
    private readonly ILogger<DocumentPlaceholderService> _logger;
    
    // Placeholder pattern: <%identifier%> where identifier contains only alphanumeric and underscore
    private static readonly Regex PlaceholderRegex = new(@"<%([a-zA-Z0-9_]+)%>", RegexOptions.Compiled);
    
    // Pattern to detect all potential placeholders (including invalid ones)
    private static readonly Regex InvalidPlaceholderPattern = new(@"<%([^%]*)%>", RegexOptions.Compiled);
    
    public DocumentPlaceholderService(ILogger<DocumentPlaceholderService> logger)
    {
        _logger = logger;
    }
    public async Task<List<string>> ExtractPlaceholdersFromDocxAsync(Stream fileStream, string fileName)
    {
        try
        {
            var placeholders = new HashSet<string>();
            using (var document = WordprocessingDocument.Open(fileStream, false))
            {
                if (document.MainDocumentPart == null)
                {
                    _logger.LogWarning("Document {FileName} has no main document part", fileName);
                    return new List<string>();
                }
                var body = document.MainDocumentPart.Document.Body;
                if (body == null)
                {
                    _logger.LogWarning("Document {FileName} has no body", fileName);
                    return new List<string>();
                }
                
                // Extract from main document body using paragraph-based extraction
                _logger.LogDebug("Extracting placeholders from main document body");
                ExtractPlaceholdersFromPart(body, placeholders);
                
                // Also check in headers and footers
                foreach (var headerPart in document.MainDocumentPart.HeaderParts)
                {
                    _logger.LogDebug("Extracting placeholders from header");
                    ExtractPlaceholdersFromPart(headerPart.Header, placeholders);
                }
                foreach (var footerPart in document.MainDocumentPart.FooterParts)
                {
                    _logger.LogDebug("Extracting placeholders from footer");
                    ExtractPlaceholdersFromPart(footerPart.Footer, placeholders);
                }
            }
            _logger.LogInformation("Extracted {Count} unique placeholders from {FileName}", 
                placeholders.Count, fileName);
            return await Task.FromResult(placeholders.OrderBy(p => p).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting placeholders from {FileName}", fileName);
            throw;
        }
    }

    public async Task<List<string>> ExtractBooleanContentControlsFromDocxAsync(Stream fileStream, string fileName)
    {
        try
        {
            var allControls = await ExtractContentControlsFromDocxAsync(fileStream, fileName);
            return allControls
                .Where(kvp => string.Equals(kvp.Value, "boolean", StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting boolean content controls in {FileName}", fileName);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> ExtractContentControlsFromDocxAsync(Stream fileStream, string fileName)
    {
        try
        {
            var contentControls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            using (var document = WordprocessingDocument.Open(fileStream, false))
            {
                if (document.MainDocumentPart == null)
                {
                    _logger.LogWarning("Document {FileName} has no main document part", fileName);
                    return contentControls;
                }

                _logger.LogDebug("Starting to extract all content controls from {FileName}", fileName);

                // Ta sama analiza co w ExtractContentControlDetailsFromDocxAsync — słownik to jej skrót:
                // wartość = typ w schemacie danych ("boolean" dla wariantu jak dotąd, "array" dla powtarzanych / list / tabel).
                foreach (var detail in CollectContentControlDetails(document))
                {
                    if (!contentControls.ContainsKey(detail.Name))
                    {
                        contentControls.Add(detail.Name, ContentControlKinds.SchemaType(detail.Kind));
                    }
                }
            }

            _logger.LogInformation("Found {Count} content controls in {FileName}", contentControls.Count, fileName);
            return await Task.FromResult(contentControls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content controls from {FileName}", fileName);
            throw;
        }
    }

    public async Task<List<ContentControlDetailDto>> ExtractContentControlDetailsFromDocxAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var document = WordprocessingDocument.Open(fileStream, false);
            if (document.MainDocumentPart == null)
            {
                _logger.LogWarning("Document {FileName} has no main document part", fileName);
                return new List<ContentControlDetailDto>();
            }

            var details = CollectContentControlDetails(document);
            _logger.LogInformation("Classified {Count} content controls in {FileName}: {Summary}", details.Count, fileName,
                string.Join(", ", details.GroupBy(d => d.Kind).Select(g => $"{g.Key}={g.Count()}")));
            return await Task.FromResult(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying content controls in {FileName}", fileName);
            throw;
        }
    }

    // ───────────────────────── klasyfikacja formantów ─────────────────────────

    private const int PreviewLength = 80;

    /// <summary>Konwencje nazw oznaczające fragment powtarzany, gdy szablon nie używa sekcji powtarzanej Worda.</summary>
    private static readonly string[] RepeatNamePrefixes = { "repeat:", "powtorz:", "powtórz:", "lista:", "list:" };

    /// <summary>
    /// Zbiera formanty tekstowe z treści, nagłówków i stopek (w tej kolejności, jak słownik) i klasyfikuje każdy:
    /// <c>repeat</c> (sekcja powtarzana Worda lub konwencja nazwy) → <c>table</c> (tabela w treści / formant wiersza lub komórki)
    /// → <c>list</c> (wszystkie akapity z numeracją) → <c>variant</c> (reszta). Pomijane: checkbox, data, listy rozwijane,
    /// obraz oraz elementy sekcji powtarzanej (w15:repeatingSectionItem — to „egzemplarz”, nie osobny formant).
    /// Nazwy zdublowane: pierwsze wystąpienie wygrywa (jak w słowniku).
    /// </summary>
    private List<ContentControlDetailDto> CollectContentControlDetails(WordprocessingDocument document)
    {
        var result = new List<ContentControlDetailDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mainPart = document.MainDocumentPart!;
        var numbering = mainPart.NumberingDefinitionsPart?.Numbering;

        void Collect(OpenXmlElement? root, string location)
        {
            if (root == null) return;
            var index = 0;
            foreach (var sdt in root.Descendants<SdtElement>())
            {
                index++;
                var detail = Classify(sdt, location, index, numbering);
                if (detail == null) continue;
                if (!seen.Add(detail.Name))
                {
                    _logger.LogWarning("Duplicate content control '{Name}' in {Location} - skipping", detail.Name, location);
                    continue;
                }
                result.Add(detail);
            }
        }

        Collect(mainPart.Document.Body, "MainDocument");
        foreach (var headerPart in mainPart.HeaderParts) Collect(headerPart.Header, "Header");
        foreach (var footerPart in mainPart.FooterParts) Collect(footerPart.Footer, "Footer");
        return result;
    }

    private ContentControlDetailDto? Classify(SdtElement sdt, string location, int index, Numbering? numbering)
    {
        var props = sdt.SdtProperties;
        if (props == null) return null;

        // Egzemplarz sekcji powtarzanej — należy do rodzica, nie jest osobnym polem
        if (props.Elements<SdtRepeatedSectionItem>().Any()) return null;

        // Checkbox / data / lista rozwijana / obraz — jak dotąd pomijane
        if (DetermineControlType(props) == null) return null;

        var tag = props.GetFirstChild<Tag>()?.Val?.Value;
        var alias = props.GetFirstChild<SdtAlias>()?.Val?.Value;
        var id = props.GetFirstChild<SdtId>()?.Val?.ToString();
        var name = !string.IsNullOrWhiteSpace(tag) ? tag
                 : !string.IsNullOrWhiteSpace(alias) ? alias
                 : !string.IsNullOrWhiteSpace(id) ? $"sdt_id_{id}"
                 : $"sdt_{index}";

        var content = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
        var paragraphs = content?.Descendants<Paragraph>().ToList() ?? new List<Paragraph>();
        // Podgląd: akapity (pozycje listy, komórki) rozdzielone separatorem, żeby tekst nie zlewał się w jedno słowo
        var text = paragraphs.Count > 0
            ? string.Join(" | ", paragraphs.Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)).Trim()).Where(t => t.Length > 0))
            : content == null ? string.Empty : string.Concat(content.Descendants<Text>().Select(t => t.Text));
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();

        var level = sdt switch
        {
            SdtRow => ContentControlLevels.Row,
            SdtCell => ContentControlLevels.Cell,
            SdtRun => ContentControlLevels.Inline,
            _ => ContentControlLevels.Block
        };

        var isRepeatingSection = props.Elements<SdtRepeatedSection>().Any();
        var nameSaysRepeat = HasRepeatNamingConvention(name);
        var containsTable = content?.Descendants<Table>().Any() == true || sdt is SdtRow || sdt is SdtCell;
        var listType = DetectListType(paragraphs, numbering);

        string kind;
        if (isRepeatingSection || nameSaysRepeat) kind = ContentControlKinds.Repeat;
        else if (containsTable) kind = ContentControlKinds.Table;
        else if (listType != null) kind = ContentControlKinds.List;
        else kind = ContentControlKinds.Variant;

        // Najbliższy nazwany formant nadrzędny (egzemplarz sekcji powtarzanej nie ma tagu — pomijamy go)
        var parentName = sdt.Ancestors<SdtElement>()
            .Select(a => a.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? a.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

        return new ContentControlDetailDto
        {
            Name = name,
            Kind = kind,
            ListType = kind == ContentControlKinds.List ? listType : null,
            Location = location,
            Level = level,
            Preview = normalized.Length == 0 ? null : normalized.Length <= PreviewLength ? normalized : normalized[..(PreviewLength - 1)] + "…",
            ParagraphCount = paragraphs.Count,
            ContainsTable = containsTable,
            ContainsList = listType != null,
            IsRepeating = isRepeatingSection || nameSaysRepeat,
            Parent = string.IsNullOrWhiteSpace(parentName) ? null : parentName,
            SuggestedWorkerType = kind == ContentControlKinds.Variant ? "Stan" : "Powtórz blok",
            Alias = !string.IsNullOrWhiteSpace(alias) && !string.Equals(alias, name, StringComparison.Ordinal) ? alias : null
        };
    }

    /// <summary>Nazwa w konwencji powtarzania: sufiks <c>[]</c> albo prefiks <c>repeat:</c> / <c>powtorz:</c> / <c>lista:</c> / <c>list:</c>.</summary>
    public static bool HasRepeatNamingConvention(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();
        return trimmed.EndsWith("[]", StringComparison.Ordinal)
            || RepeatNamePrefixes.Any(p => trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lista: co najmniej jeden akapit i wszystkie niepuste akapity mają numerację (w:numPr).
    /// Rodzaj z definicji numeracji (poziom 0 → numFmt): <c>bullet</c> albo <c>numbered</c>; bez definicji — <c>numbered</c>.
    /// </summary>
    private static string? DetectListType(IReadOnlyList<Paragraph> paragraphs, Numbering? numbering)
    {
        var meaningful = paragraphs.Where(p => !string.IsNullOrWhiteSpace(string.Concat(p.Descendants<Text>().Select(t => t.Text)))).ToList();
        if (meaningful.Count == 0) return null;
        var numbered = meaningful.Select(p => p.ParagraphProperties?.NumberingProperties).ToList();
        if (numbered.Any(n => n == null)) return null;

        var first = numbered[0]!;
        var numId = first.NumberingId?.Val?.Value;
        var ilvl = first.NumberingLevelReference?.Val?.Value ?? 0;
        if (numbering == null || numId == null) return "numbered";

        var instance = numbering.Elements<NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == numId);
        var abstractId = instance?.AbstractNumId?.Val?.Value;
        var abstractNum = abstractId == null ? null : numbering.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == abstractId);
        var lvl = abstractNum?.Elements<Level>().FirstOrDefault(l => l.LevelIndex?.Value == ilvl) ?? abstractNum?.Elements<Level>().FirstOrDefault();
        var fmt = lvl?.NumberingFormat?.Val;
        return fmt != null && fmt.Value == NumberFormatValues.Bullet ? "bullet" : "numbered";
    }

    private void ProcessAllSdtElements(IEnumerable<SdtElement> sdts, Dictionary<string, string> result, string location = "Unknown")
    {
        int processedCount = 0;
        int addedCount = 0;
        int skippedCount = 0;

        foreach (var sdt in sdts)
        {
            processedCount++;
            try
            {
                var sdtProps = sdt.SdtProperties;
                if (sdtProps == null)
                {
                    _logger.LogDebug("SDT #{Index} in {Location}: No SdtProperties found", processedCount, location);
                    skippedCount++;
                    continue;
                }

                // Get identifier from Tag, then Alias, then ID
                string? tagVal = sdtProps.GetFirstChild<Tag>()?.Val?.Value;
                string? aliasVal = sdtProps.GetFirstChild<SdtAlias>()?.Val?.Value;
                string? idVal = sdtProps.GetFirstChild<SdtId>()?.Val?.ToString();

                string identifier = !string.IsNullOrWhiteSpace(tagVal) ? tagVal
                                  : !string.IsNullOrWhiteSpace(aliasVal) ? aliasVal
                                  : !string.IsNullOrWhiteSpace(idVal) ? $"sdt_id_{idVal}"
                                  : $"sdt_{processedCount}";

                // Determine control type
                string? controlType = DetermineControlType(sdtProps);

                // Skip if type is null (not interested in this control)
                if (controlType == null)
                {
                    _logger.LogDebug("SDT #{Index} in {Location}: Identifier='{Identifier}', Type=skipped (not interested)",
                        processedCount, location, identifier);
                    skippedCount++;
                    continue;
                }

                _logger.LogDebug("SDT #{Index} in {Location}: Identifier='{Identifier}', Type='{Type}'",
                    processedCount, location, identifier, controlType);

                // Add to result (skip duplicates)
                if (!result.ContainsKey(identifier))
                {
                    result.Add(identifier, controlType);
                    addedCount++;
                }
                else
                {
                    _logger.LogWarning("SDT #{Index} in {Location}: Duplicate identifier '{Identifier}' - skipping",
                        processedCount, location, identifier);
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SDT #{Index} in {Location}: Failed to process SdtElement", processedCount, location);
                skippedCount++;
            }
        }

        _logger.LogInformation("Processed {TotalCount} SDT elements in {Location}: added {AddedCount}, skipped {SkippedCount}",
            processedCount, location, addedCount, skippedCount);
    }

    private string? DetermineControlType(SdtProperties sdtProps)
    {
        // Ignoruj natywne typy Word - nie interesują nas
        if (sdtProps.Elements<SdtContentCheckBox>().Any())
        {
            _logger.LogDebug("Skipping checkbox control - not interested");
            return null;
        }

        if (sdtProps.Elements<SdtContentDate>().Any())
        {
            _logger.LogDebug("Skipping date control - not interested");
            return null;
        }

        if (sdtProps.Elements<SdtContentDropDownList>().Any())
        {
            _logger.LogDebug("Skipping dropdown control - not interested");
            return null;
        }

        if (sdtProps.Elements<SdtContentComboBox>().Any())
        {
            _logger.LogDebug("Skipping combobox control - not interested");
            return null;
        }

        if (sdtProps.Elements<SdtContentPicture>().Any())
        {
            _logger.LogDebug("Skipping picture control - not interested");
            return null;
        }

        // Wszystkie pozostałe to Rich Text / Plain Text controls
        // Zwracamy "boolean" zamiast "text" zgodnie z wymaganiem
        _logger.LogDebug("Found text/richtext control - returning as 'boolean'");
        return "boolean";
    }

    private void ExtractPlaceholdersFromPart(OpenXmlElement element, HashSet<string> placeholders)
    {
        // Problem: Word często rozbija tekst placeholder na wiele <w:t> elementów
        // np. <w:t><%</w:t><w:t>data</w:t><w:t>%></w:t>
        // Rozwiązanie: zbieramy cały tekst z każdego paragrafu i dopiero wtedy szukamy placeholders
        
        var paragraphs = element.Descendants<Paragraph>();
        foreach (var paragraph in paragraphs)
        {
            // Zbierz cały tekst z paragrafu
            var fullText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            
            if (string.IsNullOrWhiteSpace(fullText))
                continue;
            
            // Najpierw sprawdź czy są jakiekolwiek nieprawidłowe placeholdery
            var allPotentialMatches = InvalidPlaceholderPattern.Matches(fullText);
            foreach (Match potentialMatch in allPotentialMatches)
            {
                if (!potentialMatch.Success)
                    continue;
                    
                var content = potentialMatch.Groups[1].Value;
                
                // Sprawdź czy to pusty placeholder
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("Found empty placeholder '<%%>' in document - this is invalid and will be ignored");
                    continue;
                }
                
                // Sprawdź czy zawiera spacje
                if (content.Contains(' ') || content.Contains('\t') || content.Contains('\n') || content.Contains('\r'))
                {
                    _logger.LogWarning("Found placeholder with whitespace '<%{Content}%>' in document - placeholders cannot contain spaces and will be ignored", 
                        content);
                    continue;
                }
                
                // Sprawdź czy zawiera niedozwolone znaki
                if (!Regex.IsMatch(content, @"^[a-zA-Z0-9_]+$"))
                {
                    _logger.LogWarning("Found placeholder with invalid characters '<%{Content}%>' in document - only alphanumeric and underscore are allowed", 
                        content);
                }
            }
                
            // Teraz szukaj poprawnych placeholders
            var validMatches = PlaceholderRegex.Matches(fullText);
            foreach (Match match in validMatches)
            {
                if (match.Success)
                {
                    placeholders.Add(match.Value);
                }
            }
        }
    }
}
