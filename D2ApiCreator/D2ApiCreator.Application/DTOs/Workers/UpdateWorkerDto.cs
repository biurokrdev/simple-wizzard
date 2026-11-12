namespace D2ApiCreator.Application.DTOs.Workers;

/// <summary>
/// DTO for updating a worker
/// </summary>
public class UpdateWorkerDto
{
    /// <summary>
    /// Name of the placeholder or control
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Type of worker
    /// </summary>
    public string WorkerType { get; set; } = null!;

    /// <summary>
    /// JSON configuration
    /// </summary>
    public string ConfigJson { get; set; } = null!;
}
