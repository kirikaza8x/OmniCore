namespace OmniCore.Shared.Infrastructure.Configs;

public class CorsConfig : ConfigBase
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public bool AllowAnyOrigin { get; set; } = false;
}