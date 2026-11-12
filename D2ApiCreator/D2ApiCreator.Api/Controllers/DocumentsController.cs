﻿using D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Controller for document operations
/// </summary>
public class DocumentsController : BaseApiController
{
    private readonly IDocumentValidationService _validationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentValidationService validationService,
        ILogger<DocumentsController> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Waliduje szablon dokumentu Word
    /// </summary>
    /// <param name="file">Plik DOCX do walidacji</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Wynik walidacji szablonu</returns>
    /// <response code="200">Walidacja zakończona pomyślnie</response>
    /// <response code="400">Nieprawidłowy plik</response>
    [HttpPost("validate-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateTemplate(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (!IsSupportedTemplate(file.FileName))
        {
            return BadRequest(new { message = "Only .docx and .xlsx files are supported" });
        }

        _logger.LogInformation("Validating template: {FileName}, Size: {Size} bytes", 
            file.FileName, file.Length);

        await using var stream = file.OpenReadStream();
        var result = await _validationService.ValidateDocumentTemplateAsync(
            stream, 
            file.FileName, 
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Extract placeholders and content controls from a DOCX document
    /// </summary>
    /// <param name="file">The DOCX file to process</param>
    /// <returns>
    /// Object containing:
    /// - Placeholders: List of found placeholders in format &lt;%...%&gt;
    /// - ContentControls: Dictionary of Rich Text / Plain Text content controls (always with value "boolean")
    /// </returns>
    /// <remarks>
    /// This endpoint extracts both:
    /// 1. Placeholders in format <%variable_name%>
    /// 2. Rich Text / Plain Text Content Controls (returned with type "boolean")
    /// 
    /// Other control types (checkbox, date, dropdown, etc.) are ignored.
    /// 
    /// Example response:
    /// {
    ///   "placeholders": ["<%data_aktualna%>", "<%porto%>"],
    ///   "placeholdersCount": 2,
    ///   "contentControls": {
    ///     "tag_pokaz_rozszerzony_regulamin": "boolean"
    ///   },
    ///   "contentControlsCount": 1,
    ///   "fileName": "document.docx"
    /// }
    /// </remarks>
    [HttpPost("extract-placeholders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractPlaceholders(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }
        if (!IsSupportedTemplate(file.FileName))
        {
            return BadRequest(new { message = "Only .docx and .xlsx files are supported" });
        }
        
        _logger.LogInformation("Extracting placeholders and content controls from: {FileName}", file.FileName);
        
        await using var stream = file.OpenReadStream();
        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = file.FileName
        };
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Szablony: dokument Word (.docx) albo arkusz Excel (.xlsx, od 2026-11-08).</summary>
    private static bool IsSupportedTemplate(string fileName)
        => fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
}
