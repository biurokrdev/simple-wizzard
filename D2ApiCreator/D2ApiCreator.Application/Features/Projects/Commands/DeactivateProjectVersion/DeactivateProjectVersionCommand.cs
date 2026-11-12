using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.DeactivateProjectVersion;

/// <summary>
/// Command to deactivate a project version. When the version is the project's current active
/// version, the project is left without an active version — there is no implicit fallback to
/// another version (an external system must activate one explicitly).
/// </summary>
public record DeactivateProjectVersionCommand : IRequest<Result<ProjectVersionDto>>
{
    public Guid ProjectId { get; init; }
    public Guid VersionId { get; init; }
}
