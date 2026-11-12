using D2ApiCreator.Application.Common;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.DeleteProjectVersion;

/// <summary>
/// Command to delete a project version
/// </summary>
public record DeleteProjectVersionCommand : IRequest<Result<bool>>
{
    public Guid ProjectId { get; init; }
    public Guid VersionId { get; init; }
}
