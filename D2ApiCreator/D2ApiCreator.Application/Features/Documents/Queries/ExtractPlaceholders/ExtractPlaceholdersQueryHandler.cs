using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
namespace D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
/// <summary>
/// Handler for extracting placeholders from documents
/// </summary>
public class ExtractPlaceholdersQueryHandler : IRequestHandler<ExtractPlaceholdersQuery, DocumentPlaceholderDto>
{
    private readonly IDocumentPlaceholderService _documentPlaceholderService;
    private readonly ISpreadsheetTemplateService _spreadsheetTemplateService;
    private readonly ILogger<ExtractPlaceholdersQueryHandler> _logger;
    public ExtractPlaceholdersQueryHandler(
        IDocumentPlaceholderService documentPlaceholderService,
        ISpreadsheetTemplateService spreadsheetTemplateService,
        ILogger<ExtractPlaceholdersQueryHandler> logger)
    {
        _documentPlaceholderService = documentPlaceholderService;
        _spreadsheetTemplateService = spreadsheetTemplateService;
        _logger = logger;
    }
    public async Task<DocumentPlaceholderDto> Handle(ExtractPlaceholdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting placeholders and content controls from document: {FileName}", request.FileName);

        // Szablon XLSX: osobna analiza (luźne znaczniki, nagłówki, wiersze powielane) — ten sam DTO co dla DOCX
        if (Path.GetExtension(request.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var sheet = await _spreadsheetTemplateService.AnalyzeAsync(request.FileStream, request.FileName, cancellationToken);
            return new DocumentPlaceholderDto
            {
                Placeholders = sheet.Placeholders,
                PlaceholdersCount = sheet.Placeholders.Count,
                ContentControls = sheet.Tables.ToDictionary(t => t.Name, t => ContentControlKinds.SchemaType(t.Kind), StringComparer.OrdinalIgnoreCase),
                ContentControlsCount = sheet.Tables.Count,
                ContentControlDetails = sheet.Tables,
                PlaceholderDetails = sheet.PlaceholderDetails,
                FileName = request.FileName,
                Source = "xlsx"
            };
        }
        
        // Extract placeholders <%...%>
        var placeholders = await _documentPlaceholderService.ExtractPlaceholdersFromDocxAsync(
            request.FileStream, 
            request.FileName);
        
        // Reset stream position to read again for content controls
        if (request.FileStream.CanSeek)
        {
            request.FileStream.Position = 0;
        }
        
        // Extract content controls (Rich Text / Plain Text)
        var contentControls = await _documentPlaceholderService.ExtractContentControlsFromDocxAsync(
            request.FileStream,
            request.FileName);
        
        if (request.FileStream.CanSeek)
        {
            request.FileStream.Position = 0;
        }

        // Rodzaj formantu (wariant / powtarzany / lista / tabela) — rozszerzenie, słownik wyżej zostaje dla zgodności
        var details = await _documentPlaceholderService.ExtractContentControlDetailsFromDocxAsync(
            request.FileStream,
            request.FileName) ?? new List<ContentControlDetailDto>();

        return new DocumentPlaceholderDto
        {
            Placeholders = placeholders,
            PlaceholdersCount = placeholders.Count,
            ContentControls = contentControls,
            ContentControlsCount = contentControls.Count,
            ContentControlDetails = details,
            FileName = request.FileName
        };
    }
}
