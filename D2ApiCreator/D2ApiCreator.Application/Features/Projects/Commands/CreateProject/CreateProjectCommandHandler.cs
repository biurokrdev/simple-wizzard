using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Handler for creating a new project
/// </summary>
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    public CreateProjectCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tenant, Guid> tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Generate source if not provided
            var source = string.IsNullOrWhiteSpace(request.Source)
                ? $"PRJ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
                : request.Source;

            _logger.LogInformation("Creating new project with source: {Source}", source);

            // Find tenant by ID or name (optional)
            Guid? tenantId = null;
            if (!string.IsNullOrWhiteSpace(request.TenantId))
            {
                // Try parse as GUID first
                if (Guid.TryParse(request.TenantId, out var parsedGuid))
                {
                    // Lookup by ID
                    var tenant = await _tenantRepository.GetByIdAsync(parsedGuid, cancellationToken);
                    
                    if (tenant == null)
                    {
                        _logger.LogWarning("Tenant with ID {TenantId} not found", request.TenantId);
                        return Result<ProjectDto>.Failure($"Tenant '{request.TenantId}' not found");
                    }
                    
                    tenantId = tenant.Id;
                }
                else
                {
                    // Lookup by name
                    var tenants = await _tenantRepository.FindAsync(
                        t => t.Name == request.TenantId,
                        cancellationToken);
                    var tenant = tenants.FirstOrDefault();
                    
                    if (tenant == null)
                    {
                        _logger.LogWarning("Tenant with name {TenantId} not found", request.TenantId);
                        return Result<ProjectDto>.Failure($"Tenant '{request.TenantId}' not found");
                    }
                    
                    tenantId = tenant.Id;
                }

                // Check if project with this source already exists for tenant
                var existingProjects = await _projectRepository.FindAsync(
                    p => p.TenantId == tenantId && p.Source == source,
                    cancellationToken);

                if (existingProjects.Any())
                {
                    _logger.LogWarning("Project with source {Source} already exists for tenant {TenantId}", 
                        source, request.TenantId);
                    return Result<ProjectDto>.Failure($"Project with source '{source}' already exists");
                }
            }
            else
            {
                // Check if project with this source already exists (no tenant filter)
                var existingProjects = await _projectRepository.FindAsync(
                    p => p.Source == source,
                    cancellationToken);

                if (existingProjects.Any())
                {
                    _logger.LogWarning("Project with source {Source} already exists", source);
                    return Result<ProjectDto>.Failure($"Project with source '{source}' already exists");
                }
            }

            // Build metadata from request
            var metadata = new ProjectMetadata(
                applicationName: request.ApplicationName,
                processName: request.ProcessName,
                processOwner: request.ProcessOwner,
                businessLineId: request.BusinessLineId,
                processVersion: request.ProcessVersion,
                processStep: request.ProcessStep,
                statusId: request.StatusId,
                recipientId: request.RecipientId,
                ownerCorporateKey: string.IsNullOrWhiteSpace(request.OwnerCorporateKey) ? null : request.OwnerCorporateKey.Trim()
            );

            // Create project entity
            var project = new Project(
                Guid.NewGuid(),
                tenantId,
                source,
                request.Name,
                metadata,
                request.RetentionDays,
                request.CreatedBy
            );

            if (!string.IsNullOrEmpty(request.Description))
            {
                project.UpdateDescription(request.Description);
            }

            // Add to repository
            await _projectRepository.AddAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created project {ProjectId} with source {Source}", 
                project.Id, project.Source);

            // Map to DTO (flatten metadata back to DTO for API compatibility)
            var dto = new ProjectDto
            {
                Id = project.Id,
                TenantId = tenantId,
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
                OwnerCorporateKey = project.Metadata.OwnerCorporateKey,
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
            _logger.LogError(ex, "Error creating project with source {Source}", request.Source);
            return Result<ProjectDto>.Failure($"Failed to create project: {ex.Message}");
        }
    }
}
