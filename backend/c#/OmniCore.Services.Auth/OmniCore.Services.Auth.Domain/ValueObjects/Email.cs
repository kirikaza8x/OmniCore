using OmniCore.Shared.Domain.ValueObject;

namespace OmniCore.Services.Auth.Domain.ValueObjects;

/// <summary>
/// Represents a normalized and validated email address.
/// </summary>
/// <remarks>
/// Automatically trims whitespace, converts input to lowercase, and verifies basic syntax on construction.
/// </remarks>
public class Email : ValueObject
{
    /// <summary>Gets the raw normalized email string.</summary>
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method to construct and validate an Email instance.
    /// </summary>
    /// <param name="email">The raw input email address.</param>
    /// <exception cref="ArgumentException">Thrown when input is empty or malformed.</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 256 || !normalized.Contains('@'))
        {
            throw new ArgumentException("Invalid email address format.", nameof(email));
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}