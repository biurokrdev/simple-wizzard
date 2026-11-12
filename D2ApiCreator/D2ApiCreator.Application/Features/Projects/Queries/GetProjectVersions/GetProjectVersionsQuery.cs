using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersions;

/// <summary>
/// Query to get all versions for a project
/// </summary>
public class GetProjectVersionsQuery : IRequest<Result<List<ProjectVersionDto>>>
{
    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid ProjectId { get; set; }
}
