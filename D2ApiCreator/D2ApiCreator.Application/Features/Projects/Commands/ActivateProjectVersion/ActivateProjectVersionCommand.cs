using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.ActivateProjectVersion;

/// <summary>
/// Command to activate a project version and deactivate others
/// </summary>
public record ActivateProjectVersionCommand : IRequest<Result<ProjectVersionDto>>
{
    public Guid ProjectId { get; init; }
    public Guid VersionId { get; init; }
}
