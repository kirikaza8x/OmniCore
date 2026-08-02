using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.Auth.Domain.ValueObjects;

public sealed record UserAgent
{
    public string Value { get; }

    private UserAgent(string value)
    {
        Value = value;
    }

    public static Result<UserAgent> Create(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new UserAgent("Unknown");
        }

        var trimmed = userAgent.Trim();
        if (trimmed.Length > 500)
        {
            trimmed = trimmed[..500];
        }

        return new UserAgent(trimmed);
    }

    public override string ToString() => Value;
}