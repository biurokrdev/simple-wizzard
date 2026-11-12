using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.CreateProjectVersion;

/// <summary>
/// Handler for creating a new project version
/// </summary>
public class CreateProjectVersionCommandHandler : IRequestHandler<CreateProjectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProjectVersionCommandHandler> _logger;

    public CreateProjectVersionCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateProjectVersionCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(
        CreateProjectVersionCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating new version for project {ProjectId}", request.ProjectId);

            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Project with ID {request.ProjectId} not found");
            }

            // Get existing versions to determine next version number
            var existingVersions = await _versionRepository.FindAsync(
                v => v.ProjectId == request.ProjectId,
                cancellationToken);

            int major = request.Major;
            int minor = request.Minor;

            // If version numbers are default (1.0) and already exist, find next available version
            if (request.Major == 1 && request.Minor == 0 && existingVersions.Any(v => v.Major == 1 && v.Minor == 0))
            {
                // Find the highest version number
                var maxVersion = existingVersions
                    .OrderByDescending(v => v.Major)
                    .ThenByDescending(v => v.Minor)
                    .FirstOrDefault();

                if (maxVersion != null)
                {
                    // Increment minor version
                    major = maxVersion.Major;
                    minor = maxVersion.Minor + 1;
                    _logger.LogInformation("Auto-incrementing version to {Major}.{Minor}", major, minor);
                }
            }
            // Check if requested version already exists
            else if (existingVersions.Any(v => v.Major == request.Major && v.Minor == request.Minor))
            {
                _logger.LogWarning("Version {Major}.{Minor} already exists for project {ProjectId}", 
                    request.Major, request.Minor, request.ProjectId);
                return Result<ProjectVersionDto>.Failure($"Version {request.Major}.{request.Minor} already exists for this project");
            }

            // Create project version
            var version = new ProjectVersion(
                Guid.NewGuid(),
                request.ProjectId,
                major,
                minor,
                request.CreatedBy
            );

            // Update step data if provided
            if (request.StepData != null)
            {
                version.UpdateStepData(request.StepData);
            }

            // Update notes if provided
            if (!string.IsNullOrEmpty(request.Notes))
            {
                version.UpdateNotes(request.Notes);
            }

            // Add to repository
            await _versionRepository.AddAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created version {VersionId} for project {ProjectId}", 
                version.Id, project.Id);

            // Map to DTO
            var dto = new ProjectVersionDto
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

            return Result<ProjectVersionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for project {ProjectId}", request.ProjectId);
            return Result<ProjectVersionDto>.Failure($"Failed to create project version: {ex.Message}");
        }
    }
}
