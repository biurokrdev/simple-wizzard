using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersion;

/// <summary>
/// Query to get a specific project version
/// </summary>
public class GetProjectVersionQuery : IRequest<Result<ProjectVersionDto>>
{
    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Version identifier
    /// </summary>
    public Guid VersionId { get; set; }
}
