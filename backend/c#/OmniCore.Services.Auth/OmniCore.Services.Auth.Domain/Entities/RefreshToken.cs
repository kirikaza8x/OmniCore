using System.Security.Cryptography;
using System.Text;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Represents a cryptographically secure refresh token used for JWT token rotation.
/// </summary>
/// <remarks>
/// Ensures refresh tokens are bound to specific accounts and can be explicitly revoked
/// during logout or automated reuse detection.
/// </remarks>
public class RefreshToken : Entity<RefreshTokenId>
{
    public AccountId AccountId { get; private set; } = null!;
    /// <summary>SHA-256 (lowercase hex) of the token the client holds. The raw token is never stored (ADR 0003).</summary>
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    /// <summary>Hash of the token that replaced this one when it was rotated, if any.</summary>
    public string? ReplacedByTokenHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;
    public UserSession? UserSession { get; private set; }

    private RefreshToken() { }

    private RefreshToken(RefreshTokenId id, AccountId accountId, string token, TimeSpan duration) : base(id)
    {
        AccountId = accountId;
        TokenHash = HashToken(token);
        ExpiresAtUtc = DateTime.UtcNow.Add(duration);
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Issues a new refresh token bound to an account.
    /// </summary>
    /// <param name="accountId">The owning account identifier.</param>
    /// <param name="token">The cryptographic token string.</param>
    /// <param name="duration">Lifetime duration before expiration.</param>
    /// <returns>A <see cref="Result{RefreshToken}"/> containing the instance or validation error.</returns>
    public static Result<RefreshToken> Create(AccountId accountId, string token, TimeSpan duration)
    {
        if (accountId is null)
        {
            return Result.Failure<RefreshToken>(Error.Validation("RefreshToken.AccountIdRequired", "Account ID is required."));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<RefreshToken>(Error.Validation("RefreshToken.TokenEmpty", "Token string cannot be empty."));
        }

        if (duration <= TimeSpan.Zero)
        {
            return Result.Failure<RefreshToken>(Error.Validation("RefreshToken.InvalidDuration", "Token expiration duration must be positive."));
        }

        return new RefreshToken(RefreshTokenId.New(), accountId, token, duration);
    }

    /// <summary>
    /// Revokes the token during explicit user logout or automated token reuse detection.
    /// </summary>
    /// <param name="replacedByToken">The replacement token identifier if revoked via rotation.</param>
    /// <returns>A <see cref="Result"/> indicating success or a failure state if already revoked.</returns>
    public Result Revoke(string? replacedByToken = null)
    {
        if (IsRevoked)
        {
            return Result.Failure(Error.Validation("RefreshToken.AlreadyRevoked", "Token has already been revoked."));
        }

        IsRevoked = true;
        ReplacedByTokenHash = replacedByToken is null ? null : HashToken(replacedByToken);
        return Result.Success();
    }

    /// <summary>
    /// Hashes a raw refresh token for storage and lookup. Plain SHA-256, without a salt, is right here: the token is 64
    /// random bytes, so there is nothing to brute-force or look up in a rainbow table (unlike a password).
    /// </summary>
    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>True when the raw token presented by a client is the one this row was created for.</summary>
    public bool Matches(string token) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(TokenHash),
            Encoding.ASCII.GetBytes(HashToken(token)));

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;
}