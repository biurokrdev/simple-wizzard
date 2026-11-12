namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// DTO representing found placeholders and content controls in a document
/// </summary>
public class DocumentPlaceholderDto
{
    /// <summary>
    /// Placeholders in format <%...%>
    /// </summary>
    public List<string> Placeholders { get; set; } = new();
    
    /// <summary>
    /// Content controls (Rich Text / Plain Text) with their names/tags.
    /// Key: control tag/alias, Value: schema type — "boolean" for a variant fragment (as before),
    /// "array" for repeated fragments, lists and tables (since 2026-11-08, see <see cref="ContentControlDetails"/>).
    /// </summary>
    public Dictionary<string, string> ContentControls { get; set; } = new();

    /// <summary>Rodzaj i kontekst każdego formantu (wariant / powtarzany / lista / tabela). Dodatkowe, słownik wyżej zostaje.</summary>
    public List<ContentControlDetailDto> ContentControlDetails { get; set; } = new();
    
    /// <summary>
    /// Total count of placeholders
    /// </summary>
    public int PlaceholdersCount { get; set; }
    
    /// <summary>
    /// Total count of content controls
    /// </summary>
    public int ContentControlsCount { get; set; }
    
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Rodzaj szablonu: <c>docx</c> albo <c>xlsx</c> (od 2026-11-08).</summary>
    public string Source { get; set; } = "docx";

    /// <summary>XLSX: położenie i etykieta każdego luźnego znacznika (arkusz, komórka, tekst obok). Pusta lista dla DOCX.</summary>
    public List<PlaceholderDetailDto> PlaceholderDetails { get; set; } = new();
}
