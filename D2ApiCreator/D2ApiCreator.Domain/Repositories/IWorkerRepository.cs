namespace D2ApiCreator.Domain.Repositories;

using Entities;

/// <summary>
/// Repository interface for Worker aggregate
/// </summary>
public interface IWorkerRepository : IRepository<Worker, Guid>
{
    /// <summary>
    /// Gets all workers for a specific project version
    /// </summary>
    Task<IEnumerable<Worker>> GetByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all workers for a specific project version
    /// </summary>
    Task DeleteByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default);
}
