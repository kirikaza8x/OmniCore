namespace OmniCore.Services.Notification.Domain.Enums;

/// <summary>Where a notification goes. Add a value together with an <c>INotificationChannel</c> implementation (ADR 0006).</summary>
public enum NotificationChannel
{
    Email = 1,
    /// <summary>A named, configured HTTP endpoint (Slack/Discord/Teams incoming webhook, ntfy, a custom URL).</summary>
    Webhook = 2,
}
