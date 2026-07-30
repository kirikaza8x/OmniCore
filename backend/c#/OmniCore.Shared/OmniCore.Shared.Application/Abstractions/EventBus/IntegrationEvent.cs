namespace OmniCore.Shared.Application.Abstractions.EventBus;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOnUtc { get; init; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    protected IntegrationEvent(Guid id, DateTime occurredOnUtc)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
    }
}