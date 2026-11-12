namespace D2ApiCreator.Application.Common.Interfaces;

/// <summary>
/// Application Database Context abstraction
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

