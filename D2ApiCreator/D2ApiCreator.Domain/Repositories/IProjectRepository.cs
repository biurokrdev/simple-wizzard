using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Domain.Repositories;

/// <summary>
/// Repository interface for Project aggregate
/// </summary>
public interface IProjectRepository : IRepository<Project, Guid>
{
    /// <summary>
    /// Gets project by source identifier within tenant scope
    /// </summary>
    Task<Project?> GetBySourceAsync(Guid tenantId, string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets project with all versions loaded
    /// </summary>
    Task<Project?> GetWithVersionsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets project with active version only
    /// </summary>
    Task<Project?> GetWithActiveVersionAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects for tenant with their active versions
    /// </summary>
    Task<IEnumerable<Project>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

