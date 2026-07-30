namespace OmniCore.Shared.Infrastructure.Services.Caching;

using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// Helper for constructing standard <see cref="DistributedCacheEntryOptions"/>.
/// </summary>
public static class CacheOptions
{
    /// <summary>
    /// Creates cache options based on absolute or sliding expiration guidelines.
    /// </summary>
    public static DistributedCacheEntryOptions Create(
        TimeSpan? absoluteExpiration,
        TimeSpan? slidingExpiration,
        TimeSpan defaultExpiration)
    {
        var options = new DistributedCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
        }
        else if (!slidingExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = defaultExpiration;
        }

        if (slidingExpiration.HasValue)
        {
            options.SlidingExpiration = slidingExpiration;
        }

        return options;
    }
}