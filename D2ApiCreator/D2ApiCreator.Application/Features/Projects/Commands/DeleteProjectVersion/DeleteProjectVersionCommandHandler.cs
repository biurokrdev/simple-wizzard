using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.DeleteProjectVersion;

/// <summary>
/// Handler for deleting a project version
/// </summary>
public class DeleteProjectVersionCommandHandler : IRequestHandler<DeleteProjectVersionCommand, Result<bool>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProjectVersionCommandHandler> _logger;

    public DeleteProjectVersionCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProjectVersionCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        DeleteProjectVersionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting version {VersionId} from project {ProjectId}", 
                request.VersionId, request.ProjectId);

            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<bool>.Failure($"Project with ID {request.ProjectId} not found");
            }

            // Get the version to delete
            var version = await _versionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (version == null)
            {
                _logger.LogWarning("Version {VersionId} not found", request.VersionId);
                return Result<bool>.Failure($"Version with ID {request.VersionId} not found");
            }

            // Verify version belongs to project
            if (version.ProjectId != request.ProjectId)
            {
                _logger.LogWarning("Version {VersionId} does not belong to project {ProjectId}", 
                    request.VersionId, request.ProjectId);
                return Result<bool>.Failure("Version does not belong to the specified project");
            }

            // Check if this is the active version
            if (version.IsActive)
            {
                _logger.LogWarning("Cannot delete active version {VersionId}", request.VersionId);
                return Result<bool>.Failure("Cannot delete an active version. Please activate another version first.");
            }

            // Delete the version
            await _versionRepository.DeleteAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted version {VersionId} from project {ProjectId}", 
                request.VersionId, request.ProjectId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting version {VersionId} from project {ProjectId}", 
                request.VersionId, request.ProjectId);
            return Result<bool>.Failure($"Failed to delete version: {ex.Message}");
        }
    }
}
