namespace OmniCore.Shared.Infrastructure.Configs;

public class RateLimitingConfig : ConfigBase
{
    public string GlobalPolicy { get; set; } = string.Empty;
    public Dictionary<string, FixedWindowRateLimitPolicyConfig> Policies { get; set; } = new();
}

public class FixedWindowRateLimitPolicyConfig
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 10;
}