namespace OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;

using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Application.Abstractions.Messaging;

/// <summary>
/// Renders the template and delivers it on the channel, recording the outcome. Used by the integration-event handlers and
/// the admin test endpoint. A temporary delivery failure comes back as a failure result (the caller retries); a permanent
/// one is recorded as a <c>Failed</c> notification and comes back as a success.
/// </summary>
public sealed record SendNotificationCommand(
    NotificationChannel Channel,
    string Recipient,
    string TemplateKey,
    IReadOnlyDictionary<string, string> Model,
    string? DeduplicationKey) : ICommand<NotificationResponse>;
