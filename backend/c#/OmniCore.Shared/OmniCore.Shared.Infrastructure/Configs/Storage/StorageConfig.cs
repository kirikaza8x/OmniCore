namespace OmniCore.Shared.Infrastructure.Configs.Storage;

using System.ComponentModel.DataAnnotations;

public class StorageConfig : ConfigBase
{
    public override string SectionName => "Storage";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";
    public bool UseSSL { get; set; } = true;
    public string? PublicUrl { get; set; }
}