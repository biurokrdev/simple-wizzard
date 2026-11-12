namespace D2ApiCreator.Infrastructure.Persistence.Repositories;

using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Worker entity
/// </summary>
public class WorkerRepository : Repository<Worker, Guid>, IWorkerRepository
{
    public WorkerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Worker>> GetByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.ProjectVersionId == projectVersionId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default)
    {
        var workers = await _dbSet
            .Where(w => w.ProjectVersionId == projectVersionId)
            .ToListAsync(cancellationToken);
        
        _dbSet.RemoveRange(workers);
    }
}
