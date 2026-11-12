using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Domain.Repositories;

/// <summary>Repozytorium profili workerów (katalog typów dla kreatora).</summary>
public interface IWorkerProfileRepository : IRepository<WorkerProfile, Guid>
{
    /// <summary>Wszystkie profile w kolejności <c>SortOrder</c>, <c>DisplayName</c>.</summary>
    Task<IReadOnlyList<WorkerProfile>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>Profile widoczne dla użytkowników kreatora.</summary>
    Task<IReadOnlyList<WorkerProfile>> GetVisibleAsync(CancellationToken cancellationToken = default);

    Task<WorkerProfile?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
