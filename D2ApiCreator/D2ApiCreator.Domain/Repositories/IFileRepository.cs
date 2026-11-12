namespace D2ApiCreator.Domain.Repositories;

using Entities;

/// <summary>
/// Repository interface for FileEntity aggregate
/// </summary>
public interface IFileRepository : IRepository<FileEntity, Guid>
{
    /// <summary>
    /// Gets all files for a specific project version
    /// </summary>
    Task<IEnumerable<FileEntity>> GetByProjectVersionIdAsync(Guid projectVersionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a file by project version and filename
    /// </summary>
    Task<FileEntity?> GetByProjectVersionAndFilenameAsync(Guid projectVersionId, string filename, CancellationToken cancellationToken = default);
}
