using System.Text.Json;

namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO for updating a project version
/// </summary>
public class UpdateProjectVersionDto
{
    public JsonDocument? StepData { get; set; }
    public string? Notes { get; set; }
}
