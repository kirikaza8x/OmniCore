namespace OmniCore.Shared.Domain.ValueObjects;

using System.Text.RegularExpressions;
using OmniCore.Shared.Domain.Abstractions;

public sealed record PhoneNumber
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$", // E.164 Format Standard
        RegexOptions.Compiled);

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Error.Validation("Phone.Empty", "Phone number is required.");
        }

        string cleanedPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");

        if (!PhoneRegex.IsMatch(cleanedPhone))
        {
            return Error.Validation("Phone.Invalid", "Phone number format must follow E.164 international standard.");
        }

        return new PhoneNumber(cleanedPhone);
    }

    public override string ToString() => Value;
}