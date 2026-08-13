namespace OmniCore.Services.Auth.Contracts.Events;

using OmniCore.Shared.Contracts.Events;

public record AccountRegisteredIntegrationEvent(
    Guid AccountId,
    string Username,
    string? Email) : IntegrationEvent; 