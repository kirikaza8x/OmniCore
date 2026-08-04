namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class UserSession : AggregateRoot<UserSessionId>
{
    public AccountId AccountId { get; private set; } = null!;
    public RefreshTokenId? RefreshTokenId { get; private set; }
    public string DeviceName { get; private set; } = string.Empty;
    public IpAddress IpAddress { get; private set; } = null!;
    public UserAgent UserAgent { get; private set; } = null!;
    public DateTime LastActiveAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public RefreshToken? RefreshToken { get; private set; }

    private UserSession() { }

    private UserSession(
        UserSessionId id, 
        AccountId accountId, 
        RefreshTokenId? refreshTokenId, 
        string deviceName, 
        IpAddress ipAddress, 
        UserAgent userAgent) : base(id)
    {
        AccountId = accountId;
        RefreshTokenId = refreshTokenId;
        DeviceName = deviceName;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LastActiveAtUtc = DateTime.UtcNow;
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<UserSession> Create(
        AccountId accountId, 
        RefreshTokenId? refreshTokenId, 
        string deviceName, 
        string rawIp, 
        string rawUserAgent)
    {
        var ipResult = IpAddress.Create(rawIp);
        if (ipResult.IsFailure)
        {
            return Result.Failure<UserSession>(ipResult.Error);
        }

        var userAgentResult = UserAgent.Create(rawUserAgent);
        if (userAgentResult.IsFailure)
        {
            return Result.Failure<UserSession>(userAgentResult.Error);
        }

        return new UserSession(
            UserSessionId.New(), 
            accountId, 
            refreshTokenId, 
            deviceName, 
            ipResult.Value, 
            userAgentResult.Value);
    }

    public static UserSession Create(
        AccountId accountId, 
        RefreshTokenId? refreshTokenId, 
        string deviceName, 
        IpAddress ipAddress, 
        UserAgent userAgent)
    {
        return new UserSession(
            UserSessionId.New(), 
            accountId, 
            refreshTokenId, 
            deviceName, 
            ipAddress, 
            userAgent);
    }

    public void Touch() => LastActiveAtUtc = DateTime.UtcNow;

    public void Revoke() => IsRevoked = true;
}