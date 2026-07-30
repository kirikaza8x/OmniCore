namespace OmniCore.Shared.Domain.ValueObjects;

using System.Text.RegularExpressions;
using OmniCore.Shared.Domain.Abstractions;

public sealed record EmailAddress
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("Email.Empty", "Email address is required.");
        }

        string trimmedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(trimmedEmail))
        {
            return Error.Validation("Email.Invalid", "Email address format is invalid.");
        }

        return new EmailAddress(trimmedEmail);
    }

    public override string ToString() => Value;
}