namespace OmniCore.Shared.Domain.DDD;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    TId Id { get; }
}

public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
{
    public TId Id { get; init; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id) 
            && !EqualityComparer<TId>.Default.Equals(Id, default);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}


public abstract record EntityId<TValue>(TValue Value)
{
    public override string ToString() => Value?.ToString() ?? string.Empty;
}