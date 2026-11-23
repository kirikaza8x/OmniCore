namespace OmniCore.Services.Notification.Domain.Enums;

public enum NotificationStatus
{
    /// <summary>Not delivered yet; the broker redelivers the request until it is sent or attempts run out.</summary>
    Pending = 0,
    Sent = 1,
    /// <summary>Gave up: a permanent error or the attempt limit was reached. Never retried.</summary>
    Failed = 2,
}
