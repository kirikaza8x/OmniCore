using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Represents a single-use backup recovery code for emergency multi-factor access.
/// </summary>
public class MfaRecoveryCode : Entity<MfaRecoveryCodeId>
{
    public AccountId AccountId { get; private set; } = null!;
    public string CodeHash { get; private set; } = string.Empty;
    public bool IsUsed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;

    private MfaRecoveryCode() { }

    private MfaRecoveryCode(MfaRecoveryCodeId id, AccountId accountId, string codeHash) : base(id)
    {
        AccountId = accountId;
        CodeHash = codeHash;
        IsUsed = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a single-use MFA backup recovery code hash instance.
    /// </summary>
    public static Result<MfaRecoveryCode> Create(AccountId accountId, string codeHash)
    {
        if (accountId is null)
        {
            return Result.Failure<MfaRecoveryCode>(Error.Validation("MfaRecoveryCode.AccountIdRequired", "Account ID is required."));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            return Result.Failure<MfaRecoveryCode>(Error.Validation("MfaRecoveryCode.CodeHashEmpty", "Recovery code hash cannot be empty."));
        }

        return new MfaRecoveryCode(MfaRecoveryCodeId.New(), accountId, codeHash);
    }

    /// <summary>
    /// Redeems the recovery code for emergency access, rendering it unusable for future attempts.
    /// </summary>
    public Result Redeem()
    {
        if (IsUsed)
        {
            return Result.Failure(Error.Validation("MfaRecoveryCode.AlreadyUsed", "Recovery code has already been used."));
        }

        IsUsed = true;
        return Result.Success();
    }
}