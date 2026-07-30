namespace OmniCore.Shared.Infrastructure.Configs.Database;

using System.ComponentModel.DataAnnotations;

public class DatabaseConfig : ConfigBase
{
    public override string SectionName => "Database";

    [Required(ErrorMessage = "Database connection string is required.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(5, 300)]
    public int CommandTimeout { get; set; } = 30;

    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}