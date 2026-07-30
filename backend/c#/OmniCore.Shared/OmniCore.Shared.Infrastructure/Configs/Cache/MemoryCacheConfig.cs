namespace OmniCore.Shared.Infrastructure.Configs.Cache;

using System.ComponentModel.DataAnnotations;

public class MemoryCacheConfig : ConfigBase
{
    public override string SectionName => "MemoryCache";

    /// <summary>
    /// Maximum size of the cache (number of items or memory units). Null means unlimited.
    /// </summary>
    public long? SizeLimit { get; set; }

    /// <summary>
    /// Percentage of memory to compact when SizeLimit is exceeded (0.01 to 1.0).
    /// </summary>
    [Range(0.01, 1.0)]
    public double CompactionPercentage { get; set; } = 0.05;

    [Range(1, 3600)]
    public int ExpirationScanFrequencySeconds { get; set; } = 60;

    [Range(1, 1440)]
    public int DefaultExpirationMinutes { get; set; } = 30;
}