namespace D2ApiCreator.Infrastructure.Persistence.Repositories;

using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class FileRepository : Repository<FileEntity, Guid>, IFileRepository
{
    public FileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FileEntity>> GetByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.ProjectVersionId == projectVersionId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FileEntity?> GetByProjectVersionAndFilenameAsync(Guid projectVersionId, string filename, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.ProjectVersionId == projectVersionId && f.Filename == filename, cancellationToken);
    }
}
