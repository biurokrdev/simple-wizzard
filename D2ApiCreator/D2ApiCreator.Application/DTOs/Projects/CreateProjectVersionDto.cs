using System.Text.Json;

namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO for creating a new project version with step data from wizard
/// </summary>
public class CreateProjectVersionDto
{
    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Major version number
    /// </summary>
    public int Major { get; set; } = 1;

    /// <summary>
    /// Minor version number
    /// </summary>
    public int Minor { get; set; } = 0;

    /// <summary>
    /// Business process name
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Business line identifier
    /// </summary>
    public string? BusinessLineId { get; set; }

    /// <summary>
    /// Tenant identifier (from business data) - OPTIONAL for personal projects
    /// </summary>
    public string? TenantIdentifier { get; set; }

    /// <summary>
    /// Process owner name
    /// </summary>
    public string ProcessOwner { get; set; } = string.Empty;

    /// <summary>
    /// Process version
    /// </summary>
    public string ProcessVersion { get; set; } = string.Empty;

    /// <summary>
    /// Process step
    /// </summary>
    public string ProcessStep { get; set; } = string.Empty;

    /// <summary>
    /// Status identifier
    /// </summary>
    public string StatusId { get; set; } = string.Empty;

    /// <summary>
    /// Complete step data as JSON (entire wizard state)
    /// </summary>
    public JsonDocument? StepData { get; set; }

    /// <summary>
    /// Notes/comments
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID creating the version
    /// </summary>
    public Guid? CreatedBy { get; set; }
}
