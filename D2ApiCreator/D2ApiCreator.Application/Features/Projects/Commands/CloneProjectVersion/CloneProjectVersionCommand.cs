using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.CloneProjectVersion;

/// <summary>
/// Creates a new draft version of a project as a copy of an existing one (step data, current
/// template file and workers). Used when the editor wants to change a letter whose current
/// version is active: the active version stays untouched and the work continues on the copy.
/// </summary>
public record CloneProjectVersionCommand : IRequest<Result<ProjectVersionDto>>
{
    public Guid ProjectId { get; init; }
    public Guid SourceVersionId { get; init; }
    public Guid? CreatedBy { get; init; }
}
