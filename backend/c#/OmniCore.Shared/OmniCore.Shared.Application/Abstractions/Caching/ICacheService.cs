namespace OmniCore.Shared.Application.Abstractions.Caching;

public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached item by key. Returns default if not found.
    /// </summary>
    Task<T?> GetAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with optional absolute and sliding expiration timeouts.
    /// </summary>
    Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? absoluteExpiration = null, 
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific key from the cache.
    /// </summary>
    Task RemoveAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all keys matching a prefix (e.g., "users:123:").
    /// </summary>
    Task RemoveByPrefixAsync(
        string prefix, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomic Get-or-Create pattern to prevent Cache Stampedes / Thundering Herd problems.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a key exists in cache without retrieving the full payload.
    /// </summary>
    Task<bool> ExistsAsync(
        string key, 
        CancellationToken cancellationToken = default);
}