namespace D2ApiCreator.Application.DTOs.Workers;

/// <summary>
/// DTO for worker response
/// </summary>
public class WorkerDto
{
    /// <summary>
    /// Worker ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project version ID
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// Name of the placeholder or control
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Type of worker
    /// </summary>
    public string WorkerType { get; set; } = null!;

    /// <summary>
    /// JSON configuration as string
    /// </summary>
    public string ConfigJson { get; set; } = null!;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
