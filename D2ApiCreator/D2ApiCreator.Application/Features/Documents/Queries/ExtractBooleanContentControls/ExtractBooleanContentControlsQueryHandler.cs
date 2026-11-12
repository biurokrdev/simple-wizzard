using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Documents.Queries.ExtractBooleanContentControls;

/// <summary>
/// Handler for extracting all content controls from DOCX documents with their types
/// </summary>
public class ExtractBooleanContentControlsQueryHandler : IRequestHandler<ExtractBooleanContentControlsQuery, Result<Dictionary<string, string>>>
{
    private readonly IDocumentPlaceholderService _placeholderService;
    private readonly ILogger<ExtractBooleanContentControlsQueryHandler> _logger;

    public ExtractBooleanContentControlsQueryHandler(
        IDocumentPlaceholderService placeholderService,
        ILogger<ExtractBooleanContentControlsQueryHandler> logger)
    {
        _placeholderService = placeholderService ?? throw new ArgumentNullException(nameof(placeholderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Dictionary<string, string>>> Handle(ExtractBooleanContentControlsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Extracting content controls from document: {FileName}", request.FileName);

            var contentControls = await _placeholderService.ExtractContentControlsFromDocxAsync(
                request.FileStream, 
                request.FileName);

            _logger.LogInformation("Successfully extracted {Count} content controls from {FileName}",
                contentControls.Count, request.FileName);

            return Result<Dictionary<string, string>>.Success(contentControls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content controls from document: {FileName}", request.FileName);
            return Result<Dictionary<string, string>>.Failure($"Failed to extract content controls: {ex.Message}");
        }
    }
}

