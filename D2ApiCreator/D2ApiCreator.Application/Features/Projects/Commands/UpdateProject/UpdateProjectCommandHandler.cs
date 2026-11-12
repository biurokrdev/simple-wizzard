using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProject;

/// <summary>
/// Handler for updating a project
/// </summary>
public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProjectCommandHandler> _logger;

    public UpdateProjectCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating project {ProjectId}", request.Id);

            var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.Id);
                return Result<ProjectDto>.Failure($"Project with ID {request.Id} not found");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                // Using reflection to set private property or add a method to domain entity
                var nameProperty = typeof(Project).GetProperty("Name");
                nameProperty?.SetValue(project, request.Name);
            }

            if (request.DefaultTemplateName != null)
            {
                project.UpdateTemplateName(request.DefaultTemplateName);
            }

            if (request.Description != null)
            {
                project.UpdateDescription(request.Description);
            }

            if (request.RetentionDays.HasValue)
            {
                var retentionProperty = typeof(Project).GetProperty("RetentionDays");
                retentionProperty?.SetValue(project, request.RetentionDays.Value);
            }

            await _projectRepository.UpdateAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated project {ProjectId}", project.Id);

            var dto = new ProjectDto
            {
                Id = project.Id,
                TenantId = project.TenantId,
                Source = project.Source,
                Name = project.Name,
                ApplicationName = project.Metadata.ApplicationName,
                RecipientId = project.Metadata.RecipientId,
                RetentionDays = project.RetentionDays,
                ProcessName = project.Metadata.ProcessName,
                ProcessOwner = project.Metadata.ProcessOwner,
                BusinessLineId = project.Metadata.BusinessLineId,
                ProcessVersion = project.Metadata.ProcessVersion,
                ProcessStep = project.Metadata.ProcessStep,
                StatusId = project.Metadata.StatusId,
                DefaultTemplateName = project.DefaultTemplateName,
                CreatedBy = project.CreatedBy,
                CreatedAt = project.CreatedAt,
                Description = project.Description,
                CurrentActiveVersionId = project.CurrentActiveVersionId
            };

            return Result<ProjectDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", request.Id);
            return Result<ProjectDto>.Failure($"Failed to update project: {ex.Message}");
        }
    }
}
