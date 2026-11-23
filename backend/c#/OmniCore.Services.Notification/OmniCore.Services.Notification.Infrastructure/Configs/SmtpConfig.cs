namespace OmniCore.Services.Notification.Infrastructure.Configs;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

/// <summary>Environment variables <c>Smtp__Host</c>, <c>Smtp__Port</c>, ... In dev the compose stack points this at Mailpit.</summary>
public sealed class SmtpConfig : ConfigBase
{
    public override string SectionName => "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>None (plain, Mailpit), StartTls (587), SslOnConnect (465) or Auto.</summary>
    public string Security { get; set; } = "Auto";

    public string? Username { get; set; }
    public string? Password { get; set; }

    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "OmniCore";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 20;
}
