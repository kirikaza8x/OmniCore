namespace OmniCore.Shared.Infrastructure.Configs.Qdrant;

using System.ComponentModel.DataAnnotations;

public class QdrantConfig : ConfigBase
{
    public override string SectionName => "Qdrant";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 6334;

    public bool UseHttps { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public RetryConfig Retry { get; set; } = new();

    public Dictionary<string, QdrantCollectionConfig> Collections { get; set; } = new();

    public QdrantCollectionConfig Get(string key)
        => Collections.TryGetValue(key, out var cfg)
            ? cfg
            : throw new InvalidOperationException(
                $"Qdrant collection configuration is missing for key '{key}'. " +
                $"Please define 'Qdrant:Collections:{key}' in appsettings.json.");
}

public class QdrantCollectionConfig
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int VectorSize { get; set; } = 384;
}

public class RetryConfig
{
    [Range(1, 60)]
    public int InitialDelaySeconds { get; set; } = 5;

    [Range(1, 300)]
    public int MaxDelaySeconds { get; set; } = 60;

    public bool Infinite { get; set; } = true;
}