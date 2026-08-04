namespace OmniCore.Services.Auth.Domain.ValueObjects;

using System.Text.RegularExpressions;
using OmniCore.Shared.Domain.Abstractions;

public sealed record Username
{
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9_.-]{3,30}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Username(string value) => Value = value;

    public static Result<Username> Create(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Error.Validation("Username.Empty", "Username is required.");
        }

        string trimmed = username.Trim();

        if (!UsernameRegex.IsMatch(trimmed))
        {
            return Error.Validation(
                "Username.Invalid", 
                "Username must be between 3 and 30 characters and contain only letters, numbers, underscores, dots, or hyphens.");
        }

        return new Username(trimmed);
    }

    public override string ToString() => Value;
}