using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.DeactivateProjectVersion;

/// <summary>
/// Handler for deactivating a project version (counterpart of <c>ActivateProjectVersionCommandHandler</c>).
/// Idempotent: deactivating a version that is already inactive is a no-op and does not write to the database.
/// </summary>
public class DeactivateProjectVersionCommandHandler : IRequestHandler<DeactivateProjectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateProjectVersionCommandHandler> _logger;

    public DeactivateProjectVersionCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateProjectVersionCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(
        DeactivateProjectVersionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deactivating version {VersionId} for project {ProjectId}",
                request.VersionId, request.ProjectId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Project with ID {request.ProjectId} not found");
            }

            var version = await _versionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (version == null)
            {
                _logger.LogWarning("Version {VersionId} not found", request.VersionId);
                return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
            }

            if (version.ProjectId != request.ProjectId)
            {
                _logger.LogWarning("Version {VersionId} does not belong to project {ProjectId}",
                    request.VersionId, request.ProjectId);
                return Result<ProjectVersionDto>.Failure("Version does not belong to the specified project");
            }

            var projectPointsAtVersion = project.CurrentActiveVersionId == request.VersionId;

            if (!version.IsActive && !projectPointsAtVersion)
            {
                // Already inactive — nothing to change (keeps Draft versions as Draft, no write).
                _logger.LogInformation("Version {VersionId} is already inactive — no-op", request.VersionId);
                return Result<ProjectVersionDto>.Success(MapToDto(version));
            }

            if (version.IsActive)
            {
                version.Deactivate();
                await _versionRepository.UpdateAsync(version, cancellationToken);
            }

            // The pointer is cleared even when the flag was already false, so an inconsistent
            // (pointer set, flag false) pair cannot survive a deactivation request.
            if (projectPointsAtVersion)
            {
                project.ClearActiveVersion();
                await _projectRepository.UpdateAsync(project, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deactivated version {VersionId} for project {ProjectId}",
                request.VersionId, request.ProjectId);

            return Result<ProjectVersionDto>.Success(MapToDto(version));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating version {VersionId} for project {ProjectId}",
                request.VersionId, request.ProjectId);
            return Result<ProjectVersionDto>.Failure($"Failed to deactivate version: {ex.Message}");
        }
    }

    private static ProjectVersionDto MapToDto(ProjectVersion version) => new()
    {
        Id = version.Id,
        ProjectId = version.ProjectId,
        Major = version.Major,
        Minor = version.Minor,
        VersionTag = version.VersionTag,
        IsActive = version.IsActive,
        Status = version.Status,
        CreatedBy = version.CreatedBy,
        CreatedAt = version.CreatedAt,
        StepData = version.StepData,
        Notes = version.Notes
    };
}
