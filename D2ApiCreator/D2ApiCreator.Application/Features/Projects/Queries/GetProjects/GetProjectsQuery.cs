using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjects;

/// <summary>
/// Query to get projects with pagination and filtering
/// </summary>
public class GetProjectsQuery : IRequest<Result<PagedResult<ProjectDto>>>
{
    /// <summary>
    /// Optional tenant ID to filter projects
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional filter by source
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Search term for filtering by name or source
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by creation date from
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter by creation date to
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Corporate key of requesting user (for membership filtering)
    /// </summary>
    public string? CurrentUserCorporateKey { get; set; }
}
