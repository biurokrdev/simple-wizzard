using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersion;

/// <summary>
/// Handler for updating a project version
/// </summary>
public class UpdateProjectVersionCommandHandler : IRequestHandler<UpdateProjectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProjectVersionCommandHandler> _logger;

    public UpdateProjectVersionCommandHandler(
        IRepository<ProjectVersion, Guid> versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProjectVersionCommandHandler> logger)
    {
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(
        UpdateProjectVersionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating project version {VersionId}", request.Id);

            var version = await _versionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (version == null)
            {
                _logger.LogWarning("Project version {VersionId} not found", request.Id);
                return Result<ProjectVersionDto>.Failure($"Project version with ID {request.Id} not found");
            }

            // Update fields if provided
            if (request.StepData != null)
            {
                version.UpdateStepData(request.StepData);
            }

            if (request.Notes != null)
            {
                version.UpdateNotes(request.Notes);
            }

            await _versionRepository.UpdateAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated project version {VersionId}", version.Id);

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
            _logger.LogError(ex, "Error updating project version {VersionId}", request.Id);
            return Result<ProjectVersionDto>.Failure($"Failed to update project version: {ex.Message}");
        }
    }
}
