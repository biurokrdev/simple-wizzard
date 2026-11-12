using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProject;

/// <summary>
/// Query to get a single project by ID
/// </summary>
public class GetProjectQuery : IRequest<Result<ProjectDto>>
{
    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid Id { get; set; }
}
