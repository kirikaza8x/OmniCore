namespace OmniCore.Services.Auth.Application.Features.Auth.EventHandlers;

using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Application.Abstractions.Messaging;

public sealed class PublishAccountRegisteredIntegrationEventHandler(
    IEventBus eventBus,
    ILogger<PublishAccountRegisteredIntegrationEventHandler> logger)
    : IDomainEventHandler<AccountCreatedDomainEvent>
{
    public async Task HandleAsync(
        AccountCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publishing AccountRegisteredIntegrationEvent for Account ID: {AccountId}",
            domainEvent.AccountId.Value);

        var integrationEvent = new AccountRegisteredIntegrationEvent(
            domainEvent.AccountId.Value,
            domainEvent.Username, 
            domainEvent.Email);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}