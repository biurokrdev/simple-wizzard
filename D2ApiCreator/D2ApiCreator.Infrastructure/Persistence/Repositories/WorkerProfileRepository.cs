using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace D2ApiCreator.Infrastructure.Persistence.Repositories;

public class WorkerProfileRepository : Repository<WorkerProfile, Guid>, IWorkerProfileRepository
{
    public WorkerProfileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<WorkerProfile>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayName).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerProfile>> GetVisibleAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.IsVisible).OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayName).ToListAsync(cancellationToken);
    }

    public Task<WorkerProfile?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
    }
}
