namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO for updating a project
/// </summary>
public class UpdateProjectDto
{
    public string? Name { get; set; }
    public string? DefaultTemplateName { get; set; }
    public string? Description { get; set; }
    public int? RetentionDays { get; set; }
}
