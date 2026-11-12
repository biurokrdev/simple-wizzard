using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProject;

/// <summary>
/// Handler for getting a single project
/// </summary>
public class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, Result<ProjectDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly ILogger<GetProjectQueryHandler> _logger;

    public GetProjectQueryHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tenant, Guid> tenantRepository,
        ILogger<GetProjectQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Result<ProjectDto>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting project with ID: {ProjectId}", request.Id);

            var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning("Project not found: {ProjectId}", request.Id);
                return Result<ProjectDto>.Failure($"Project with ID {request.Id} not found");
            }

            var tenant = project.TenantId.HasValue
                ? await _tenantRepository.GetByIdAsync(project.TenantId.Value, cancellationToken)
                : null;

            var dto = new ProjectDto
            {
                Id = project.Id,
                TenantId = project.TenantId,
                TenantName = tenant?.Name,
                OwnerCorporateKey = project.Metadata.OwnerCorporateKey,
                ApplicationName = project.Metadata.ApplicationName,
                RecipientId = project.Metadata.RecipientId,
                ProcessName = project.Metadata.ProcessName,
                ProcessOwner = project.Metadata.ProcessOwner,
                BusinessLineId = project.Metadata.BusinessLineId,
                ProcessVersion = project.Metadata.ProcessVersion,
                ProcessStep = project.Metadata.ProcessStep,
                StatusId = project.Metadata.StatusId,
                Source = project.Source,
                Name = project.Name,
                DefaultTemplateName = project.DefaultTemplateName,
                CreatedBy = project.CreatedBy,
                CreatedAt = project.CreatedAt,
                RetentionDays = project.RetentionDays,
                Description = project.Description,
                CurrentActiveVersionId = project.CurrentActiveVersionId
            };

            _logger.LogInformation("Successfully retrieved project: {ProjectId}", request.Id);

            return Result<ProjectDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", request.Id);
            return Result<ProjectDto>.Failure($"Error getting project: {ex.Message}");
        }
    }
}
