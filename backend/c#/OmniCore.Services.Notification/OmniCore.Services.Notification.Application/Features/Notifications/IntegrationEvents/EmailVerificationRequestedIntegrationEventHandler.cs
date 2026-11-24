namespace OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Contracts.Events;

/// <summary>
/// Sends the "verify-email" e-mail. The event carries a raw single-use token: it goes only into the link in the model, which
/// the delivery log does not store, and it is never logged here (ADR 0009). The dedup key is the event id, so a broker
/// redelivery does not send a second mail while a new request (a new event) does.
/// </summary>
public sealed class EmailVerificationRequestedIntegrationEventHandler(
    ISender sender,
    INotificationLinks links,
    ILogger<EmailVerificationRequestedIntegrationEventHandler> logger)
    : IntegrationEventHandler<EmailVerificationRequestedIntegrationEvent>
{
    public override async Task HandleAsync(EmailVerificationRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SendNotificationCommand(
                NotificationChannel.Email,
                integrationEvent.Email,
                "verify-email",
                new Dictionary<string, string>
                {
                    ["name"] = integrationEvent.Username,
                    ["appName"] = "OmniCore",
                    ["link"] = links.VerifyEmail(integrationEvent.Token),
                    ["expiresInMinutes"] = integrationEvent.ExpiresInMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                DeduplicationKey: $"verify-email:{integrationEvent.Id}"),
            cancellationToken);

        NotificationEventOutcome.ThrowIfRetryable(result, logger, nameof(EmailVerificationRequestedIntegrationEvent), integrationEvent.Id);
    }
}
