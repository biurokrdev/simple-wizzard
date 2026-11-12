using Microsoft.EntityFrameworkCore;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Infrastructure.Persistence;

namespace D2ApiCreator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of IProjectVersionRepository using EF Core
/// </summary>
public class ProjectVersionRepository : Repository<ProjectVersion, Guid>, IProjectVersionRepository
{
    public ProjectVersionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ProjectVersion?> GetActiveVersionAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pv => pv.Project)
            .Include(pv => pv.Creator)
            .FirstOrDefaultAsync(pv => pv.ProjectId == projectId && pv.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<ProjectVersion>> GetVersionHistoryAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pv => pv.ProjectId == projectId)
            .Include(pv => pv.Creator)
            .OrderByDescending(pv => pv.Major)
            .ThenByDescending(pv => pv.Minor)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectVersion?> GetWithDetailsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pv => pv.Project)
            .Include(pv => pv.Creator)
            .Include(pv => pv.Files)
                .ThenInclude(f => f.ParseResults)
            .Include(pv => pv.Mappings.OrderBy(m => m.SortOrder))
            .Include(pv => pv.Approvals)
                .ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(pv => pv.Id == versionId, cancellationToken);
    }

    public async Task<(int Major, int Minor)?> GetLatestVersionNumberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var latest = await _dbSet
            .Where(pv => pv.ProjectId == projectId)
            .OrderByDescending(pv => pv.Major)
            .ThenByDescending(pv => pv.Minor)
            .Select(pv => new { pv.Major, pv.Minor })
            .FirstOrDefaultAsync(cancellationToken);

        return latest != null ? (latest.Major, latest.Minor) : null;
    }

    public async Task<bool> VersionTagExistsAsync(Guid projectId, string versionTag, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(pv => pv.ProjectId == projectId && pv.VersionTag == versionTag, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectVersionTemplateInfo>> GetTemplateInfoByProjectIdsAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken = default)
    {
        if (projectIds.Count == 0)
        {
            return Array.Empty<ProjectVersionTemplateInfo>();
        }

        // Projection only — no file content is loaded (templates live in GCS, `content` is legacy).
        return await _dbSet
            .Where(pv => projectIds.Contains(pv.ProjectId))
            .Select(pv => new ProjectVersionTemplateInfo(
                pv.Id,
                pv.ProjectId,
                pv.VersionTag,
                pv.Major,
                pv.Minor,
                pv.IsActive,
                pv.CreatedAt,
                pv.Files.OrderByDescending(f => f.CreatedAt).Select(f => f.Filename).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}

