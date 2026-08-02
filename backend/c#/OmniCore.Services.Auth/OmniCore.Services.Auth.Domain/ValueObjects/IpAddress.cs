using System.Net;
using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.Auth.Domain.ValueObjects;

public sealed record IpAddress
{
    public string Value { get; }

    private IpAddress(string value)
    {
        Value = value;
    }

    public static Result<IpAddress> Create(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return new IpAddress("0.0.0.0");
        }

        var trimmed = ip.Trim();
        if (!IPAddress.TryParse(trimmed, out _))
        {
            return Error.Validation("IpAddress.Invalid", "Invalid IP address format.");
        }

        return new IpAddress(trimmed);
    }

    public override string ToString() => Value;
}