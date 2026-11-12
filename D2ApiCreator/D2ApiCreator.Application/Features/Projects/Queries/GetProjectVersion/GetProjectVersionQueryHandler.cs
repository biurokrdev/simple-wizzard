using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersion;

/// <summary>
/// Handler for getting a specific project version
/// </summary>
public class GetProjectVersionQueryHandler : IRequestHandler<GetProjectVersionQuery, Result<ProjectVersionDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly ILogger<GetProjectVersionQueryHandler> _logger;

    public GetProjectVersionQueryHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        ILogger<GetProjectVersionQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(GetProjectVersionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting project version. ProjectId: {ProjectId}, VersionId: {VersionId}", 
                request.ProjectId, request.VersionId);

            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project not found: {ProjectId}", request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Project with ID {request.ProjectId} not found");
            }

            // Get version
            var version = await _versionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (version == null)
            {
                _logger.LogWarning("Version not found: {VersionId}", request.VersionId);
                return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
            }

            // Verify version belongs to project
            if (version.ProjectId != request.ProjectId)
            {
                _logger.LogWarning("Version {VersionId} does not belong to project {ProjectId}", 
                    request.VersionId, request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Version does not belong to the specified project");
            }

            var dto = ProjectVersionDtoMapper.ToDto(version);

            _logger.LogInformation("Successfully retrieved project version: {VersionId}", request.VersionId);

            return Result<ProjectVersionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project version. ProjectId: {ProjectId}, VersionId: {VersionId}", 
                request.ProjectId, request.VersionId);
            return Result<ProjectVersionDto>.Failure($"Error getting project version: {ex.Message}");
        }
    }
}
