using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Stores immutable security event records for incident response and compliance auditing.
/// </summary>
public class SecurityAuditLog : Entity<SecurityAuditLogId>
{
    public AccountId? AccountId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public IpAddress IpAddress { get; private set; } = null!;
    public UserAgent UserAgent { get; private set; } = null!;
    public string? MetadataJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Account? Account { get; private set; }

    private SecurityAuditLog() { }

    private SecurityAuditLog(
        SecurityAuditLogId id, 
        AccountId? accountId, 
        string eventType, 
        IpAddress ipAddress, 
        UserAgent userAgent, 
        string? metadataJson) : base(id)
    {
        AccountId = accountId;
        EventType = eventType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        MetadataJson = metadataJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new audit log entry from raw string values.
    /// </summary>
    /// <returns>A <see cref="Result{SecurityAuditLog}"/> containing the log or validation failure errors.</returns>
    public static Result<SecurityAuditLog> Log(
        AccountId? accountId, 
        string eventType, 
        string rawIp, 
        string rawUserAgent, 
        string? metadataJson = null)
    {
        var ipResult = IpAddress.Create(rawIp);
        if (ipResult.IsFailure)
        {
            return Result.Failure<SecurityAuditLog>(ipResult.Error);
        }

        var userAgentResult = UserAgent.Create(rawUserAgent);
        if (userAgentResult.IsFailure)
        {
            return Result.Failure<SecurityAuditLog>(userAgentResult.Error);
        }

        return new SecurityAuditLog(
            SecurityAuditLogId.New(), 
            accountId, 
            eventType, 
            ipResult.Value, 
            userAgentResult.Value, 
            metadataJson);
    }

    /// <summary>
    /// Factory overload accepting already constructed Value Objects directly.
    /// </summary>
    public static SecurityAuditLog Log(
        AccountId? accountId, 
        string eventType, 
        IpAddress ipAddress, 
        UserAgent userAgent, 
        string? metadataJson = null)
    {
        return new SecurityAuditLog(
            SecurityAuditLogId.New(), 
            accountId, 
            eventType, 
            ipAddress, 
            userAgent, 
            metadataJson);
    }
}