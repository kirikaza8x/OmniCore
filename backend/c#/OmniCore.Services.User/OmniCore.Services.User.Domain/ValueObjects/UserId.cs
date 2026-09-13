// ValueObjects/UserId.cs
namespace OmniCore.Services.User.Domain.ValueObjects;

using OmniCore.Shared.Domain.ValueObject;

public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId From(Guid value) => new(value);
    public static UserId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}