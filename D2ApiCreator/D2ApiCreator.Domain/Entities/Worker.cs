namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;
using System.Text.Json;

/// <summary>
/// Represents a worker for mapping template fields (placeholders and content controls)
/// </summary>
public class Worker : AggregateRoot<Guid>
{
    protected Worker() { }

    public Worker(Guid id, Guid projectVersionId, string name, string workerType, JsonDocument configJson)
        : base(id)
    {
        ProjectVersionId = projectVersionId;
        Name = name;
        WorkerType = workerType;
        ConfigJson = configJson;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Project version this worker belongs to
    /// </summary>
    public Guid ProjectVersionId { get; private set; }

    /// <summary>
    /// Name of the placeholder or control being mapped (e.g., "<%data_aktualna%>" or "tag_pokaz_rozszerzony_regulamin")
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Type of worker: "Podstaw wartość" for placeholders, "Stan" for text controls
    /// </summary>
    public string WorkerType { get; private set; } = null!;

    /// <summary>
    /// JSON configuration for the worker mapping
    /// </summary>
    public JsonDocument ConfigJson { get; private set; } = null!;

    /// <summary>
    /// When this worker was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this worker was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public ProjectVersion ProjectVersion { get; private set; } = null!;

    /// <summary>
    /// Update worker configuration
    /// </summary>
    public void UpdateConfiguration(string name, string workerType, JsonDocument configJson)
    {
        Name = name;
        WorkerType = workerType;
        ConfigJson = configJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
