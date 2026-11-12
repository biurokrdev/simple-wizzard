using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersionStepData;

public class UpdateProjectVersionStepDataCommandHandler : IRequestHandler<UpdateProjectVersionStepDataCommand, Result<bool>>
{
    private readonly IProjectVersionRepository _projectVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProjectVersionStepDataCommandHandler> _logger;

    public UpdateProjectVersionStepDataCommandHandler(
        IProjectVersionRepository projectVersionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProjectVersionStepDataCommandHandler> logger)
    {
        _projectVersionRepository = projectVersionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateProjectVersionStepDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating step data for project version {ProjectVersionId}", request.ProjectVersionId);

            var projectVersion = await _projectVersionRepository.GetByIdAsync(request.ProjectVersionId, cancellationToken);
            if (projectVersion == null)
            {
                _logger.LogWarning("Project version {ProjectVersionId} not found", request.ProjectVersionId);
                return Result<bool>.Failure($"Project version {request.ProjectVersionId} not found");
            }

            // Update step data
            projectVersion.UpdateStepData(request.StepData);

            // Update notes if provided
            if (request.Notes != null)
            {
                projectVersion.UpdateNotes(request.Notes);
            }

            await _projectVersionRepository.UpdateAsync(projectVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated step data for project version {ProjectVersionId}", request.ProjectVersionId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating step data for project version {ProjectVersionId}", request.ProjectVersionId);
            return Result<bool>.Failure($"Failed to update step data: {ex.Message}");
        }
    }
}
