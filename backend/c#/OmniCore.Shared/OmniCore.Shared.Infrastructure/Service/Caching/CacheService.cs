namespace OmniCore.Shared.Infrastructure.Services.Caching;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Infrastructure.Configs.Cache;
using StackExchange.Redis;

/// <summary>
/// High-performance distributed caching service supporting cache stampede prevention and Redis prefix invalidation.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly MemoryCacheConfig _config;
    private readonly ILogger<CacheService> _logger;
    private readonly IConnectionMultiplexer? _redisConnection;

    // Fixed key-lock tracking structure to eliminate lock leaks under high load
    private static readonly ConcurrentDictionary<string, RefCountedLock> KeyLocks = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    public CacheService(
        IDistributedCache cache,
        IOptions<MemoryCacheConfig> config,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _cache = cache;
        _config = config.Value;
        _logger = logger;
        _redisConnection = redisConnection;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            byte[]? bytes = await _cache.GetAsync(key, cancellationToken);
            return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve cache key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (value is null) return;

        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var defaultTtl = TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
            var options = CacheOptions.Create(absoluteExpiration, slidingExpiration, defaultTtl);

            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (_redisConnection is null)
        {
            _logger.LogWarning("RemoveByPrefixAsync called for '{Prefix}', but Redis ConnectionMultiplexer is not configured.", prefix);
            return;
        }

        try
        {
            var endpoints = _redisConnection.GetEndPoints();
            var pattern = $"{prefix}*";

            foreach (var endpoint in endpoints)
            {
                var server = _redisConnection.GetServer(endpoint);
                if (!server.IsReplica)
                {
                    // Scan keys non-blockingly
                    var keys = server.Keys(pattern: pattern).ToArray();
                    if (keys.Length > 0)
                    {
                        var db = _redisConnection.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                        _logger.LogInformation("Removed {Count} keys matching prefix: {Prefix}", keys.Length, prefix);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache keys by prefix: {Prefix}", prefix);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            byte[]? bytes = await _cache.GetAsync(key, cancellationToken);
            return bytes is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache key existence: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        // Fast path: Return cached value if present
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        // Thread-safe lock retrieval with reference counting
        var lockObj = KeyLocks.AddOrUpdate(
            key,
            _ => new RefCountedLock(),
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await lockObj.Semaphore.WaitAsync(cancellationToken);

        try
        {
            // Double-check cache inside lock
            cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue is not null)
            {
                return cachedValue;
            }

            var value = await factory(cancellationToken);

            if (value is not null)
            {
                await SetAsync(key, value, absoluteExpiration, slidingExpiration, cancellationToken);
            }

            return value;
        }
        finally
        {
            lockObj.Semaphore.Release();

            // Safely decrement and clean up dictionary entry without race conditions
            if (Interlocked.Decrement(ref lockObj.RefCount) == 0)
            {
                KeyLocks.TryRemove(new KeyValuePair<string, RefCountedLock>(key, lockObj));
            }
        }
    }

    private sealed class RefCountedLock
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount = 1;
    }
}