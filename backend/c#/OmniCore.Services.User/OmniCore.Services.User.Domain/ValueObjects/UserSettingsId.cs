// ValueObjects/UserSettingsId.cs
namespace OmniCore.Services.User.Domain.ValueObjects;

using OmniCore.Shared.Domain.ValueObject;

public sealed class UserSettingsId : ValueObject
{
    public Guid Value { get; }

    private UserSettingsId(Guid value)
    {
        Value = value;
    }

    public static UserSettingsId From(Guid value) => new(value);
    public static UserSettingsId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}