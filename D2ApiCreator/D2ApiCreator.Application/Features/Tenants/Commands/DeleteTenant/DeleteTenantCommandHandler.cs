using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result<bool>>
{
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        IRepository<Tenant, Guid> tenantRepository,
        IRepository<Project, Guid> projectRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result<bool>.Failure("Tenant not found");

            // Only creator can delete
            if (tenant.CreatedBy != request.RequesterId)
                return Result<bool>.Failure("Only tenant creator can delete the tenant");

            // Check if there are any projects associated
            var projects = await _projectRepository.FindAsync(
                p => p.TenantId == request.TenantId,
                cancellationToken);

            if (projects.Any())
                return Result<bool>.Failure("Cannot delete tenant with associated projects");

            await _tenantRepository.DeleteAsync(tenant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted tenant {TenantId}", request.TenantId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant");
            return Result<bool>.Failure($"Error deleting tenant: {ex.Message}");
        }
    }
}
