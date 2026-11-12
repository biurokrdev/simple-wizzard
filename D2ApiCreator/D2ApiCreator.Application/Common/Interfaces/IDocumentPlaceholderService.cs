using D2ApiCreator.Application.DTOs;

namespace D2ApiCreator.Application.Common.Interfaces;
/// <summary>
/// Service for extracting placeholders from documents
/// </summary>
public interface IDocumentPlaceholderService
{
    /// <summary>
    /// Extracts all placeholders in format &lt;%...%&gt; from a DOCX file
    /// </summary>
    /// <param name="fileStream">The DOCX file stream</param>
    /// <param name="fileName">The name of the file</param>
    /// <returns>List of found placeholders</returns>
    Task<List<string>> ExtractPlaceholdersFromDocxAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Extracts Rich Text / Plain Text content controls from a DOCX file
    /// </summary>
    /// <param name="fileStream">The DOCX file stream</param>
    /// <param name="fileName">The name of the file</param>
    /// <returns>Dictionary where key is control tag/alias and value is always "boolean" for text controls. Other control types are ignored.</returns>
    Task<Dictionary<string, string>> ExtractContentControlsFromDocxAsync(Stream fileStream, string fileName);
    
    /// <summary>
    /// Extracts Rich Text / Plain Text content controls from a DOCX file (returns list of names)
    /// </summary>
    /// <param name="fileStream">The DOCX file stream</param>
    /// <param name="fileName">The name of the file</param>
    /// <returns>List of control names/tags</returns>
    Task<List<string>> ExtractBooleanContentControlsFromDocxAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Rozszerzona analiza formantów tekstowych: rodzaj (wariant / powtarzany / lista / tabela), miejsce, poziom,
    /// podgląd treści, zagnieżdżenie. Uzupełnia <see cref="ExtractContentControlsFromDocxAsync"/> — nie zastępuje go.
    /// </summary>
    Task<List<ContentControlDetailDto>> ExtractContentControlDetailsFromDocxAsync(Stream fileStream, string fileName);
}
