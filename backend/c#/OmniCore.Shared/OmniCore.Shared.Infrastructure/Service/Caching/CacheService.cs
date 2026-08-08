namespace OmniCore.Shared.Infrastructure.Services.Caching;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Infrastructure.Configs.Cache;
using StackExchange.Redis;

/// <summary>
/// High-performance caching service with Redis priority, automatic In-Memory fallback, 
/// accurate provider hit/miss/set logging, and stampede protection.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheConfig _config;
    private readonly ILogger<CacheService> _logger;
    private readonly IConnectionMultiplexer? _redisConnection;

    private readonly string _distributedProviderName;

    private static readonly ConcurrentDictionary<string, RefCountedLock> KeyLocks = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public CacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        IOptions<MemoryCacheConfig> config,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _config = config.Value;
        _logger = logger;
        _redisConnection = redisConnection;

        _distributedProviderName = distributedCache.GetType().Name.Contains("Redis", StringComparison.OrdinalIgnoreCase)
            ? "Redis"
            : "Distributed-Memory";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // 1. Try Primary Distributed Cache
        try
        {
            byte[]? bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (bytes is not null)
            {
                // Deserialize FIRST before logging a successful hit
                var result = JsonSerializer.Deserialize<T>(bytes, JsonOptions);

                _logger.LogInformation("Cache HIT [{Provider}] for key '{Key}'", _distributedProviderName, key);
                return result;
            }

            _logger.LogInformation("Cache MISS [{Provider}] for key '{Key}'", _distributedProviderName, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} GET failed or deserialization error for key '{Key}'. Falling back to In-Memory cache.", _distributedProviderName, key);

            // 2. Fallback to Local In-Memory Cache on Failure
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                _logger.LogInformation("Cache HIT [Memory-Fallback] for key '{Key}'", key);
                return memoryValue;
            }

            _logger.LogInformation("Cache MISS [Memory-Fallback] for key '{Key}'", key);
            return default;
        }

        return default;
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

        var defaultTtl = TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
        var effectiveAbsoluteTtl = absoluteExpiration ?? defaultTtl;

        // 1. Try Primary Distributed Cache
        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = CacheOptions.Create(absoluteExpiration, slidingExpiration, defaultTtl);

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);

            _logger.LogInformation("Cache SET [{Provider}] for key '{Key}' (TTL: {Minutes:F1}m)", _distributedProviderName, key, effectiveAbsoluteTtl.TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} SET failed for key '{Key}'. Falling back to In-Memory cache.", _distributedProviderName, key);

            // 2. Fallback to In-Memory Cache on Failure
            SetInMemory(key, value, effectiveAbsoluteTtl, slidingExpiration);

            _logger.LogInformation("Cache SET [Memory-Fallback] for key '{Key}' (TTL: {Minutes:F1}m)", key, effectiveAbsoluteTtl.TotalMinutes);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _memoryCache.Remove(key);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogInformation("Cache REMOVE [{Provider}] for key '{Key}'", _distributedProviderName, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} REMOVE failed for key '{Key}'. Cleared from In-Memory fallback.", _distributedProviderName, key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (_redisConnection is not null && _redisConnection.IsConnected)
        {
            try
            {
                var endpoints = _redisConnection.GetEndPoints();
                var pattern = $"{prefix}*";

                foreach (var endpoint in endpoints)
                {
                    var server = _redisConnection.GetServer(endpoint);
                    if (!server.IsReplica)
                    {
                        var keys = server.Keys(pattern: pattern).ToArray();
                        if (keys.Length > 0)
                        {
                            var db = _redisConnection.GetDatabase();
                            await db.KeyDeleteAsync(keys);
                            _logger.LogInformation("Removed {Count} Redis keys matching prefix: '{Prefix}'", keys.Length, prefix);
                        }
                    }
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis RemoveByPrefixAsync failed for prefix: '{Prefix}'.", prefix);
            }
        }

        _logger.LogWarning("Redis connection unavailable. Prefix removal for '{Prefix}' skipped for In-Memory fallback (keys will auto-expire).", prefix);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            byte[]? bytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (bytes is not null)
            {
                _logger.LogInformation("Cache HIT [{Provider}] for key '{Key}'", _distributedProviderName, key);
                return true;
            }

            _logger.LogInformation("Cache MISS [{Provider}] for key '{Key}'", _distributedProviderName, key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} EXISTS failed for key '{Key}'. Falling back to In-Memory cache.", _distributedProviderName, key);

            if (_memoryCache.TryGetValue(key, out _))
            {
                _logger.LogInformation("Cache HIT [Memory-Fallback] for key '{Key}'", key);
                return true;
            }

            _logger.LogInformation("Cache MISS [Memory-Fallback] for key '{Key}'", key);
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

        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

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
            // Double-check inside lock
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

            if (Interlocked.Decrement(ref lockObj.RefCount) == 0)
            {
                KeyLocks.TryRemove(new KeyValuePair<string, RefCountedLock>(key, lockObj));
            }
        }
    }

    private void SetInMemory<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan? slidingExpiration)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        };

        if (slidingExpiration.HasValue)
        {
            entryOptions.SlidingExpiration = slidingExpiration.Value;
        }

        _memoryCache.Set(key, value, entryOptions);
    }

    private sealed class RefCountedLock
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount = 1;
    }
}