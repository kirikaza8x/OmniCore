namespace OmniCore.Shared.Domain.DDD;

public interface ITenantScoped
{
    TenantId TenantId { get; }
}
public readonly record struct TenantId
{
    public string Value { get; }

    public TenantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tenant identifier cannot be null or empty.", nameof(value));
        }

        Value = value.ToLowerInvariant().Trim();
    }

    public static TenantId From(string value) => new(value);
    public static implicit operator string(TenantId tenantId) => tenantId.Value;
    
    public override string ToString() => Value;
}

public abstract class TenantEntity<TId> : Entity<TId>, ITenantScoped
{
    public TenantId TenantId { get; init; }

    protected TenantEntity() { }

    protected TenantEntity(TId id, TenantId tenantId) : base(id)
    {
        TenantId = tenantId;
    }
}

public abstract class TenantAggregateRoot<TId> : AggregateRoot<TId>, ITenantScoped
{
    public TenantId TenantId { get; init; }

    protected TenantAggregateRoot() { }

    protected TenantAggregateRoot(TId id, TenantId tenantId) : base(id)
    {
        TenantId = tenantId;
    }
}