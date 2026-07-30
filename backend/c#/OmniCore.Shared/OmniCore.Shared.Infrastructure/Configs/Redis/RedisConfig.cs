namespace OmniCore.Shared.Infrastructure.Configs.Redis;

using System.ComponentModel.DataAnnotations;

public class RedisConfig : ConfigBase
{
    public override string SectionName => "Redis";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 6379;

    public string? Password { get; set; }
    public string InstanceName { get; set; } = "OmniCore_";

    public string ConnectionString => string.IsNullOrWhiteSpace(Password)
        ? $"{Host}:{Port},abortConnect=false"
        : $"{Host}:{Port},password={Password},abortConnect=false";
}