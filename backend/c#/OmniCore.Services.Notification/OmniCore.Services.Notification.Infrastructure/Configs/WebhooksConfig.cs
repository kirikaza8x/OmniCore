namespace OmniCore.Services.Notification.Infrastructure.Configs;

using OmniCore.Shared.Infrastructure.Configs;

/// <summary>
/// Named webhook endpoints: <c>Webhooks__Endpoints__ops=https://hooks.example/...</c>. A request names an endpoint, it never
/// supplies a URL, so nobody can make the service call an arbitrary address. The URLs are secrets and are never logged.
/// </summary>
public sealed class WebhooksConfig : ConfigBase
{
    public override string SectionName => "Webhooks";

    public Dictionary<string, string> Endpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int TimeoutSeconds { get; set; } = 10;
}
