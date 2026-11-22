namespace OmniCore.Services.User.Application.Features.Profiles.IntegrationEvents;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;
using OmniCore.Shared.Contracts.Events;

/// <summary>Creates the profile when Auth registers an account (finding U6, ADR 0004).</summary>
public sealed class AccountRegisteredIntegrationEventHandler(
    ISender sender,
    ILogger<AccountRegisteredIntegrationEventHandler> logger)
    : IntegrationEventHandler<AccountRegisteredIntegrationEvent>
{
    public override async Task HandleAsync(
        AccountRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        // Auth does not collect names and fills FirstName with the username (AC2): do not store that as a real name.
        var firstName = string.Equals(integrationEvent.FirstName, integrationEvent.Username, StringComparison.OrdinalIgnoreCase)
            ? null
            : integrationEvent.FirstName;

        var result = await sender.Send(
            new ProvisionUserProfileCommand(
                integrationEvent.AccountId,
                integrationEvent.Username,
                integrationEvent.Email,
                firstName,
                integrationEvent.LastName),
            cancellationToken);

        if (result.IsFailure)
        {
            // Throwing makes MassTransit retry and then park the message in the error queue instead of losing it.
            logger.LogError(
                "Could not create a profile for account {AccountId}: {Error}",
                integrationEvent.AccountId, result.Error);
            throw new InvalidOperationException(
                $"Could not create a profile for account {integrationEvent.AccountId}: {result.Error.Code}");
        }
    }
}
