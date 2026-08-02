namespace OmniCore.Shared.Application.Abstractions.EventBus;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}

