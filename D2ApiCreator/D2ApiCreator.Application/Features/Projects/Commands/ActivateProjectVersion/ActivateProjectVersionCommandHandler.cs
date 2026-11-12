using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.ActivateProjectVersion;

/// <summary>
/// Handler for activating a project version
/// </summary>
public class ActivateProjectVersionCommandHandler : IRequestHandler<ActivateProjectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateProjectVersionCommandHandler> _logger;

    public ActivateProjectVersionCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<ActivateProjectVersionCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(
        ActivateProjectVersionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Activating version {VersionId} for project {ProjectId}", 
                request.VersionId, request.ProjectId);

            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Project with ID {request.ProjectId} not found");
            }

            // Get the version to activate
            var versionToActivate = await _versionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (versionToActivate == null)
            {
                _logger.LogWarning("Version {VersionId} not found", request.VersionId);
                return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
            }

            // Verify version belongs to project
            if (versionToActivate.ProjectId != request.ProjectId)
            {
                _logger.LogWarning("Version {VersionId} does not belong to project {ProjectId}", 
                    request.VersionId, request.ProjectId);
                return Result<ProjectVersionDto>.Failure("Version does not belong to the specified project");
            }

            // Deactivate all other versions for this project
            var allVersions = await _versionRepository.FindAsync(
                v => v.ProjectId == request.ProjectId,
                cancellationToken);

            foreach (var version in allVersions)
            {
                if (version.Id != request.VersionId && version.IsActive)
                {
                    version.Deactivate();
                    await _versionRepository.UpdateAsync(version, cancellationToken);
                }
            }

            // Activate the selected version
            versionToActivate.Activate();
            await _versionRepository.UpdateAsync(versionToActivate, cancellationToken);

            // Update project's current active version
            project.SetActiveVersion(request.VersionId);
            await _projectRepository.UpdateAsync(project, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully activated version {VersionId} for project {ProjectId}", 
                request.VersionId, request.ProjectId);

            var dto = new ProjectVersionDto
            {
                Id = versionToActivate.Id,
                ProjectId = versionToActivate.ProjectId,
                Major = versionToActivate.Major,
                Minor = versionToActivate.Minor,
                VersionTag = versionToActivate.VersionTag,
                IsActive = versionToActivate.IsActive,
                Status = versionToActivate.Status,
                CreatedBy = versionToActivate.CreatedBy,
                CreatedAt = versionToActivate.CreatedAt,
                StepData = versionToActivate.StepData,
                Notes = versionToActivate.Notes
            };

            return Result<ProjectVersionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating version {VersionId} for project {ProjectId}", 
                request.VersionId, request.ProjectId);
            return Result<ProjectVersionDto>.Failure($"Failed to activate version: {ex.Message}");
        }
    }
}
