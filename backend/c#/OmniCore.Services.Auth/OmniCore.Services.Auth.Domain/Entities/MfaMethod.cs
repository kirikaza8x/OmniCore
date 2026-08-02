using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Stores configured Multi-Factor Authentication credentials (e.g., TOTP, Passkey) for an account.
/// </summary>
public class MfaMethod : Entity<MfaMethodId>
{
    public AccountId AccountId { get; private set; } = null!;
    public MfaType Type { get; private set; }

    /// <summary>Gets the encrypted secret string used for TOTP calculation.</summary>
    public string Secret { get; private set; } = string.Empty;

    public bool IsVerified { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;

    private MfaMethod() { }

    private MfaMethod(MfaMethodId id, AccountId accountId, MfaType type, string secret) : base(id)
    {
        AccountId = accountId;
        Type = type;
        Secret = secret;
        IsVerified = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Registers a new MFA setup configuration.
    /// </summary>
    /// <param name="accountId">The owner account identifier.</param>
    /// <param name="type">The multi-factor mechanism type.</param>
    /// <param name="encryptedSecret">Encrypted secret key string.</param>
    /// <returns>A <see cref="Result{MfaMethod}"/> instance.</returns>
    public static Result<MfaMethod> Create(AccountId accountId, MfaType type, string encryptedSecret)
    {
        if (accountId is null)
        {
            return Result.Failure<MfaMethod>(Error.Validation("MfaMethod.AccountIdRequired", "Account ID is required."));
        }

        if (string.IsNullOrWhiteSpace(encryptedSecret))
        {
            return Result.Failure<MfaMethod>(Error.Validation("MfaMethod.SecretEmpty", "Encrypted MFA secret cannot be empty."));
        }

        return new MfaMethod(MfaMethodId.New(), accountId, type, encryptedSecret);
    }

    /// <summary>
    /// Confirms successful verification of the MFA setup.
    /// </summary>
    public Result Verify()
    {
        if (IsVerified)
        {
            return Result.Failure(Error.Validation("MfaMethod.AlreadyVerified", "MFA method is already verified."));
        }

        IsVerified = true;
        return Result.Success();
    }
}

