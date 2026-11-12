using Microsoft.AspNetCore.Http;

namespace D2ApiCreator.Application.DTOs.Files;

/// <summary>
/// DTO for uploading a file (template) to a project version
/// </summary>
public class UploadFileDto
{
    /// <summary>
    /// Project version identifier
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// File to upload
    /// </summary>
    public IFormFile File { get; set; } = null!;
    
    /// <summary>
    /// Optional file type identifier (e.g., "template", "schema", "attachment")
    /// </summary>
    public string? FileType { get; set; }
}
