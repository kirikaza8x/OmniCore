namespace OmniCore.Services.Notification.Application.Abstractions;

using OmniCore.Services.Notification.Domain.Enums;

/// <summary>
/// One way of delivering a notification. Implementations live in Infrastructure and are registered as
/// <c>INotificationChannel</c>; the send handler picks the one whose <see cref="Channel"/> matches. They never throw for
/// delivery problems: they return a <see cref="DeliveryResult"/> saying whether trying again can help.
/// </summary>
public interface INotificationChannel
{
    NotificationChannel Channel { get; }

    Task<DeliveryResult> SendAsync(OutgoingNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>What a channel receives: already rendered. For Webhook, <see cref="Recipient"/> is the configured endpoint name.</summary>
public sealed record OutgoingNotification(string Recipient, string Subject, string Body, bool IsHtml);

public sealed record DeliveryResult(bool Succeeded, bool IsPermanent, string? Error)
{
    public static DeliveryResult Ok { get; } = new(true, false, null);

    /// <summary>Worth retrying: the server was down, timed out, rate-limited us.</summary>
    public static DeliveryResult Transient(string error) => new(false, false, error);

    /// <summary>Retrying cannot help: bad address, unknown endpoint, rejected by the server.</summary>
    public static DeliveryResult Permanent(string error) => new(false, true, error);
}
