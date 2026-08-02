using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.Auth.Domain.ValueObjects;

public sealed record PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static Result<PasswordHash> Create(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Error.Validation("PasswordHash.Empty", "Password hash cannot be empty.");
        }

        return new PasswordHash(hash);
    }

    public override string ToString() => Value;
}