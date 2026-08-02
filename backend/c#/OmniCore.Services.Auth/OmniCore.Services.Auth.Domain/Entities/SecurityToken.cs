using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Represents single-use, short-lived security codes for email confirmation, password resets, or OTPs.
/// </summary>
/// <remarks>
/// Plaintext security codes must NEVER be stored directly; only SHA256 hashes are preserved.
/// </remarks>
public class SecurityToken : Entity<SecurityTokenId>
{
    public AccountId AccountId { get; private set; } = null!;

    /// <summary>Gets the SHA256 hash of the verification code.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    public SecurityTokenType TokenType { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;

    private SecurityToken() { }

    private SecurityToken(
        SecurityTokenId id, 
        AccountId accountId, 
        string codeHash, 
        SecurityTokenType tokenType, 
        TimeSpan lifetime) : base(id)
    {
        AccountId = accountId;
        CodeHash = codeHash;
        TokenType = tokenType;
        ExpiresAtUtc = DateTime.UtcNow.Add(lifetime);
        IsUsed = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to construct a short-lived security token.
    /// </summary>
    /// <param name="accountId">The owner account identifier.</param>
    /// <param name="codeHash">Hashed security verification code.</param>
    /// <param name="tokenType">The intended scope/purpose of the token.</param>
    /// <param name="lifetime">Validity duration before expiration.</param>
    /// <returns>A <see cref="Result{SecurityToken}"/> instance.</returns>
    public static Result<SecurityToken> Create(
        AccountId accountId, 
        string codeHash, 
        SecurityTokenType tokenType, 
        TimeSpan lifetime)
    {
        if (accountId is null)
        {
            return Result.Failure<SecurityToken>(Error.Validation("SecurityToken.AccountIdRequired", "Account ID is required."));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            return Result.Failure<SecurityToken>(Error.Validation("SecurityToken.CodeHashEmpty", "Code hash cannot be empty."));
        }

        if (lifetime <= TimeSpan.Zero)
        {
            return Result.Failure<SecurityToken>(Error.Validation("SecurityToken.InvalidLifetime", "Lifetime duration must be positive."));
        }

        return new SecurityToken(SecurityTokenId.New(), accountId, codeHash, tokenType, lifetime);
    }

    /// <summary>
    /// Marks the token as consumed so it cannot be reused.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or a failure state if already used/expired.</returns>
    public Result MarkAsUsed()
    {
        if (IsUsed)
        {
            return Result.Failure(Error.Validation("SecurityToken.AlreadyUsed", "Security token has already been used."));
        }

        if (IsExpired)
        {
            return Result.Failure(Error.Validation("SecurityToken.Expired", "Security token has expired."));
        }

        IsUsed = true;
        return Result.Success();
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}