// ValueObjects/UserAddressId.cs
namespace OmniCore.Services.User.Domain.ValueObjects;

using OmniCore.Shared.Domain.ValueObject;

public sealed class UserAddressId : ValueObject
{
    public Guid Value { get; }

    private UserAddressId(Guid value)
    {
        Value = value;
    }

    public static UserAddressId From(Guid value) => new(value);
    public static UserAddressId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}