namespace OmniCore.Services.Notification.Infrastructure.Configs;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

/// <summary>Environment variable <c>Notification__PublicBaseUrl</c>: where the web client is reachable by users.</summary>
public sealed class NotificationLinksConfig : ConfigBase
{
    public override string SectionName => "Notification";

    /// <summary>Absolute http(s) URL without a path, for example https://app.example.com.</summary>
    [Required]
    public string PublicBaseUrl { get; set; } = "http://localhost:3000";
}
