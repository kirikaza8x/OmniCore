namespace OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Contracts.Events;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Contracts.Events;

/// <summary>The generic entry point: any service publishes <see cref="NotificationRequestedIntegrationEvent"/>.</summary>
public sealed class NotificationRequestedIntegrationEventHandler(
    ISender sender,
    ILogger<NotificationRequestedIntegrationEventHandler> logger)
    : IntegrationEventHandler<NotificationRequestedIntegrationEvent>
{
    public override async Task HandleAsync(NotificationRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationChannel>(integrationEvent.Channel, ignoreCase: true, out var channel) || !Enum.IsDefined(channel))
        {
            // Retrying cannot fix an unknown channel: drop it loudly instead of filling the error queue.
            logger.LogWarning("Notification request {EventId} dropped: unknown channel '{Channel}'", integrationEvent.Id, integrationEvent.Channel);
            return;
        }

        var result = await sender.Send(
            new SendNotificationCommand(
                channel,
                integrationEvent.Recipient,
                integrationEvent.TemplateKey,
                integrationEvent.Model ?? new Dictionary<string, string>(),
                integrationEvent.DeduplicationKey ?? $"event:{integrationEvent.Id}"),
            cancellationToken);

        NotificationEventOutcome.ThrowIfRetryable(result, logger, nameof(NotificationRequestedIntegrationEvent), integrationEvent.Id);
    }
}
