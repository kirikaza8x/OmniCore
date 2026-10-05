// ValueObjects/UserTenantProfileId.cs
namespace OmniCore.Services.User.Domain.ValueObjects;

using OmniCore.Shared.Domain.ValueObject;

public sealed class UserTenantProfileId : ValueObject
{
    public Guid Value { get; }

    private UserTenantProfileId(Guid value)
    {
        Value = value;
    }

    public static UserTenantProfileId From(Guid value) => new(value);
    public static UserTenantProfileId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}