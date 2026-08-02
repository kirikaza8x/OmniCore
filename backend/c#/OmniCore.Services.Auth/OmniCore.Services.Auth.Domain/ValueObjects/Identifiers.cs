using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.ValueObjects;

/// <summary>
/// Strongly-typed unique identifier for an Account.
/// </summary>
/// <remarks>
/// Prevents accidental parameter swapping bugs at compile-time (e.g., passing a RoleId into an AccountId parameter).
/// </remarks>
public record AccountId(Guid Value) : EntityId<Guid>(Value)
{
    /// <summary>Creates a new random <see cref="AccountId"/>.</summary>
    public static AccountId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for a RefreshToken entity.</summary>
public record RefreshTokenId(Guid Value) : EntityId<Guid>(Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for an ExternalLogin entity.</summary>
public record ExternalLoginId(Guid Value) : EntityId<Guid>(Value)
{
    public static ExternalLoginId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for a SecurityToken entity.</summary>
public record SecurityTokenId(Guid Value) : EntityId<Guid>(Value)
{
    public static SecurityTokenId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for an MfaMethod entity.</summary>
public record MfaMethodId(Guid Value) : EntityId<Guid>(Value)
{
    public static MfaMethodId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for an MfaRecoveryCode entity.</summary>
public record MfaRecoveryCodeId(Guid Value) : EntityId<Guid>(Value)
{
    public static MfaRecoveryCodeId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for a UserSession entity.</summary>
public record UserSessionId(Guid Value) : EntityId<Guid>(Value)
{
    public static UserSessionId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for a SecurityAuditLog entity.</summary>
public record SecurityAuditLogId(Guid Value) : EntityId<Guid>(Value)
{
    public static SecurityAuditLogId New() => new(Guid.NewGuid());
}

/// <summary>Strongly-typed identifier for a Role entity.</summary>
public record RoleId(Guid Value) : EntityId<Guid>(Value)
{
    public static RoleId New() => new(Guid.NewGuid());
}