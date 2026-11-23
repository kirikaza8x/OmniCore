namespace OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Contracts.Events;

/// <summary>Sends the welcome e-mail when Auth registers an account. One welcome per account, even if the event is delivered twice.</summary>
public sealed class AccountRegisteredIntegrationEventHandler(
    ISender sender,
    ILogger<AccountRegisteredIntegrationEventHandler> logger)
    : IntegrationEventHandler<AccountRegisteredIntegrationEvent>
{
    public override async Task HandleAsync(AccountRegisteredIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        // Auth fills FirstName with the username when it has no real name (AC2): greet by whichever reads better.
        var name = string.IsNullOrWhiteSpace(integrationEvent.FirstName) ? integrationEvent.Username : integrationEvent.FirstName;

        var result = await sender.Send(
            new SendNotificationCommand(
                NotificationChannel.Email,
                integrationEvent.Email,
                "welcome",
                new Dictionary<string, string> { ["name"] = name, ["appName"] = "OmniCore" },
                DeduplicationKey: $"welcome:{integrationEvent.AccountId}"),
            cancellationToken);

        NotificationEventOutcome.ThrowIfRetryable(result, logger, nameof(AccountRegisteredIntegrationEvent), integrationEvent.Id);
    }
}
