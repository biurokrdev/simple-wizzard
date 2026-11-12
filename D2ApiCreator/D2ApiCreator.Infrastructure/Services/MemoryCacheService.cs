using Microsoft.Extensions.Caching.Memory;

namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Interfejs dla serwisu cache
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Pobiera wartość z cache lub ją tworzy jeśli nie istnieje
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);

    /// <summary>
    /// Pobiera wartość z cache
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Ustawia wartość w cache
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);

    /// <summary>
    /// Usuwa wartość z cache
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Odświeża wartość w cache - usuwa i tworzy ponownie
    /// </summary>
    Task<T> RefreshAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);

    /// <summary>
    /// Czyści cały cache
    /// </summary>
    void Clear();
}

/// <summary>
/// Implementacja serwisu cache wykorzystująca IMemoryCache
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys = new();
    private readonly object _lockObject = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory();
        Set(key, value, absoluteExpiration);
        return value;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }
        else
        {
            // Domyślnie 1 godzina
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        }

        _cache.Set(key, value, options);

        lock (_lockObject)
        {
            _keys.Add(key);
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        lock (_lockObject)
        {
            _keys.Remove(key);
        }
    }

    public async Task<T> RefreshAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
    {
        Remove(key);
        var value = await factory();
        Set(key, value, absoluteExpiration);
        return value;
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            foreach (var key in _keys)
            {
                _cache.Remove(key);
            }
            _keys.Clear();
        }
    }
}
