using D2ApiCreator.Application.DTOs;

namespace D2ApiCreator.Application.DTOs.Workers;

/// <summary>
/// DTO for generating workers from document analysis
/// </summary>
public class GenerateWorkersDto
{
    /// <summary>
    /// Project version ID
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// List of placeholders from document (e.g., ["<%data_aktualna%>", "<%porto%>"])
    /// </summary>
    public List<string> Placeholders { get; set; } = new();

    /// <summary>
    /// Dictionary of content controls from document (key: control name, value: type)
    /// </summary>
    public Dictionary<string, string> ContentControls { get; set; } = new();

    /// <summary>
    /// Opcjonalnie: rodzaj formantów z analizy szablonu (<c>extract-placeholders</c>). Gdy podany, decyduje o typie workera
    /// (wariant → „Stan”, powtarzany / lista / tabela → „Powtórz blok”). Bez niego decyduje wartość w <see cref="ContentControls"/>
    /// ("boolean" → „Stan”, "array" → „Powtórz blok”), więc starsi klienci działają jak dotąd.
    /// </summary>
    public List<ContentControlDetailDto>? ContentControlDetails { get; set; }

    /// <summary>Opcjonalnie (XLSX): arkusz, komórka i etykieta luźnych znaczników — trafiają do konfiguracji „Podstaw wartość”.</summary>
    public List<PlaceholderDetailDto>? PlaceholderDetails { get; set; }
}
