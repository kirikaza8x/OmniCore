namespace OmniCore.Shared.Domain.DDD;

public interface IAggregateRoot : IEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    IReadOnlyCollection<IDomainEvent> ClearDomainEvents();
}

public interface IAggregateRoot<TId> : IAggregateRoot, IEntity<TId>
{
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> ClearDomainEvents()
    {
        var clearedEvents = _domainEvents.ToArray();
        _domainEvents.Clear();
        return clearedEvents;
    }
}