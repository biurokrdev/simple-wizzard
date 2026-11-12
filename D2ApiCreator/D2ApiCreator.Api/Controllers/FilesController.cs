using D2ApiCreator.Application.DTOs.Files;
using D2ApiCreator.Application.Features.Files.Commands.UploadFile;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Controller for file management (templates, schemas, attachments)
/// </summary>
public class FilesController : BaseApiController
{
    private readonly ILogger<FilesController> _logger;

    public FilesController(ILogger<FilesController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload a file to a project version
    /// </summary>
    /// <param name="projectVersionId">Project version identifier</param>
    /// <param name="file">File to upload</param>
    /// <param name="fileType">Optional file type (e.g., "template", "schema")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File identifier</returns>
    /// <response code="201">File uploaded successfully</response>
    /// <response code="400">Invalid file or request data</response>
    /// <response code="404">Project version not found</response>
    [HttpPost("project-versions/{projectVersionId}/upload")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadFile(
        Guid projectVersionId,
        IFormFile file,
        [FromQuery] string? fileType,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required and cannot be empty" });
        }

        // Validate file extension
        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) && !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only .docx and .xlsx files are allowed" });
        }

        // Validate file size (10MB max)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size cannot exceed 10MB" });
        }

        _logger.LogInformation("Uploading file {FileName} ({Size} bytes) to project version {ProjectVersionId}", 
            file.FileName, file.Length, projectVersionId);

        var command = new UploadFileCommand
        {
            ProjectVersionId = projectVersionId,
            File = file,
            FileType = fileType
        };

        try
        {
            var fileId = await Mediator.Send(command, cancellationToken);

            var response = new FileUploadResponse
            {
                FileId = fileId,
                FileName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            return CreatedAtAction(
                nameof(GetFile),
                new { fileId },
                response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to project version {ProjectVersionId}", 
                file.FileName, projectVersionId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Get file by ID (placeholder for CreatedAtAction)
    /// </summary>
    [HttpGet("{fileId}")]
    [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid fileId)
    {
        // TODO: Implement GetFileQuery
        await Task.CompletedTask;
        return NotFound(new { message = "Get file endpoint not yet implemented" });
    }
}

/// <summary>
/// Response for file upload
/// </summary>
public class FileUploadResponse
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
}
