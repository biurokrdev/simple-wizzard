using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Domain.Repositories;

/// <summary>
/// Repository interface for ProjectVersion aggregate
/// </summary>
public interface IProjectVersionRepository : IRepository<ProjectVersion, Guid>
{
    /// <summary>
    /// Gets active version for project
    /// </summary>
    Task<ProjectVersion?> GetActiveVersionAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets version history for project ordered by version (descending)
    /// </summary>
    Task<IEnumerable<ProjectVersion>> GetVersionHistoryAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets version with all related data (files, mappings, approvals)
    /// </summary>
    Task<ProjectVersion?> GetWithDetailsAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latest version number for project (to help with version incrementing)
    /// </summary>
    Task<(int Major, int Minor)?> GetLatestVersionNumberAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if version tag exists for project
    /// </summary>
    Task<bool> VersionTagExistsAsync(Guid projectId, string versionTag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every version of the given projects as a light projection (number, active flag,
    /// template filename) — used by project lists to show the template of the active or newest draft version.
    /// </summary>
    Task<IReadOnlyList<ProjectVersionTemplateInfo>> GetTemplateInfoByProjectIdsAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken = default);
}

