// ValueObjects/Username.cs
namespace OmniCore.Services.User.Domain.ValueObjects;

using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.ValueObject;

public sealed class Username : ValueObject
{
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    public static Result<Username> Create(string rawUsername)
    {
        if (string.IsNullOrWhiteSpace(rawUsername))
        {
            return Result.Failure<Username>(Error.Validation("Username.Empty", "Username cannot be empty."));
        }

        var trimmed = rawUsername.Trim();
        if (trimmed.Length is < 3 or > 30)
        {
            return Result.Failure<Username>(Error.Validation("Username.InvalidLength", "Username must be between 3 and 30 characters."));
        }

        return new Username(trimmed);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}