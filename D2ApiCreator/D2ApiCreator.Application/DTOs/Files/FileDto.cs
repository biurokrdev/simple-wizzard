namespace D2ApiCreator.Application.DTOs.Files;

/// <summary>
/// DTO representing a file entity
/// </summary>
public class FileDto
{
    public Guid Id { get; set; }
    public Guid ProjectVersionId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public string? Sha256 { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? StorageRef { get; set; }
}
