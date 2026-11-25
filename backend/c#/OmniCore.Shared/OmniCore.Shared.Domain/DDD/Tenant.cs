namespace OmniCore.Shared.Domain.DDD;

public interface ITenantScoped
{
    TenantId TenantId { get; }
}
/// <summary>Tenant identifier (ADR 0001, a Guid stored as uuid). The empty value is never a valid tenant.</summary>
public readonly record struct TenantId
{
    public Guid Value { get; }

    public TenantId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId From(Guid value) => new(value);

    public static bool TryParse(string? text, out TenantId tenantId)
    {
        if (Guid.TryParse(text, out Guid guid) && guid != Guid.Empty)
        {
            tenantId = new TenantId(guid);
            return true;
        }

        tenantId = default;
        return false;
    }

    public override string ToString() => Value.ToString();
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