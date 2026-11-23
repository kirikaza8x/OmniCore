namespace OmniCore.Services.Notification.Application.DTOs;

using OmniCore.Services.Notification.Domain.Entities;

public sealed record NotificationResponse(
    Guid Id,
    string Channel,
    string Recipient,
    string TemplateKey,
    string? Subject,
    string Status,
    int Attempts,
    string? LastError,
    DateTime CreatedAt,
    DateTime? LastAttemptAt,
    DateTime? SentAt);

public static class NotificationMappings
{
    public static NotificationResponse ToResponse(this NotificationMessage m) => new(
        m.Id.Value, m.Channel.ToString(), m.Recipient, m.TemplateKey, m.Subject, m.Status.ToString(),
        m.Attempts, m.LastError, m.CreatedAt, m.LastAttemptAt, m.SentAt);
}
