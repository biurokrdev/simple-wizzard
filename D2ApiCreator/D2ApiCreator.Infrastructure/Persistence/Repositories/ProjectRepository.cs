using Microsoft.EntityFrameworkCore;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Infrastructure.Persistence;

namespace D2ApiCreator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of IProjectRepository using EF Core
/// </summary>
public class ProjectRepository : Repository<Project, Guid>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetBySourceAsync(Guid tenantId, string source, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Source == source, cancellationToken);
    }

    public async Task<Project?> GetWithVersionsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Versions.OrderByDescending(v => v.Major).ThenByDescending(v => v.Minor))
            .Include(p => p.Creator)
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    public async Task<Project?> GetWithActiveVersionAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Versions.Where(v => v.IsActive))
            .Include(p => p.Creator)
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Versions.Where(v => v.IsActive))
            .Include(p => p.Creator)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

