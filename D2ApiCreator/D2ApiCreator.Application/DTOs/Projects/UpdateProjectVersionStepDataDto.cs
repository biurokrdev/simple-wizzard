using System.Text.Json;

namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO for updating project version step data
/// </summary>
public class UpdateProjectVersionStepDataDto
{
    /// <summary>
    /// Project version identifier
    /// </summary>
    public Guid ProjectVersionId { get; set; }

    /// <summary>
    /// Complete step data as JSON (entire wizard state)
    /// </summary>
    public JsonDocument StepData { get; set; } = null!;

    /// <summary>
    /// Optional notes/comments
    /// </summary>
    public string? Notes { get; set; }
}
