using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersions;

/// <summary>
/// Handler for getting all versions of a project
/// </summary>
public class GetProjectVersionsQueryHandler : IRequestHandler<GetProjectVersionsQuery, Result<List<ProjectVersionDto>>>
{
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly ILogger<GetProjectVersionsQueryHandler> _logger;

    public GetProjectVersionsQueryHandler(
        IRepository<ProjectVersion, Guid> versionRepository,
        ILogger<GetProjectVersionsQueryHandler> logger)
    {
        _versionRepository = versionRepository ?? throw new ArgumentNullException(nameof(versionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<ProjectVersionDto>>> Handle(GetProjectVersionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all versions for project: {ProjectId}", request.ProjectId);

        // Get all versions for the project, ordered by creation date descending (newest first)
        var versions = await _versionRepository.FindAsync(
            v => v.ProjectId == request.ProjectId,
            cancellationToken);

        var versionsList = versions
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Major)
            .ThenByDescending(v => v.Minor)
            .ToList();

        _logger.LogInformation("Found {Count} versions for project {ProjectId}", versionsList.Count, request.ProjectId);

        var dtos = versionsList.Select(ProjectVersionDtoMapper.ToDto).ToList();

        return Result<List<ProjectVersionDto>>.Success(dtos);
    }
}
