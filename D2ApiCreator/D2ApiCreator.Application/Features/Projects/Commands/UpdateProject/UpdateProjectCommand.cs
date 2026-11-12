using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProject;

/// <summary>
/// Command to update an existing project
/// </summary>
public record UpdateProjectCommand : IRequest<Result<ProjectDto>>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? DefaultTemplateName { get; init; }
    public string? Description { get; init; }
    public int? RetentionDays { get; init; }
}
