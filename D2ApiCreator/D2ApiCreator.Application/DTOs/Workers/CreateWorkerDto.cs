namespace D2ApiCreator.Application.DTOs.Workers;

/// <summary>
/// DTO for creating a new worker
/// </summary>
public class CreateWorkerDto
{
    /// <summary>
    /// Project version ID
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// Name of the placeholder or control (e.g., "<%data_aktualna%>")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Type of worker: "Podstaw wartość" or "Stan"
    /// </summary>
    public string WorkerType { get; set; } = null!;

    /// <summary>
    /// JSON configuration for the worker
    /// </summary>
    public string ConfigJson { get; set; } = null!;
}
