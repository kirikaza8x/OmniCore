namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.ValueObjects;

/// <summary>
/// Aggregate root representing an authentication account and its security boundary.
/// </summary>
public partial class Account : AggregateRoot<AccountId>, IAuditableEntity, ISoftDeletable
{
    public Username Username { get; private set; } = null!;
    public EmailAddress? Email { get; private set; }
    public PasswordHash? PasswordHash { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }

    // ISoftDeletable Implementation
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    // IAuditableEntity Implementation
    public DateTime? CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    // Navigation Collections (Encapsulated)
    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<AccountRole> _accountRoles = [];
    public IReadOnlyCollection<AccountRole> AccountRoles => _accountRoles.AsReadOnly();

    public ICollection<ExternalLogin> ExternalLogins { get; private set; } = new List<ExternalLogin>();
    public ICollection<SecurityToken> SecurityTokens { get; private set; } = new List<SecurityToken>();
    public ICollection<MfaMethod> MfaMethods { get; private set; } = new List<MfaMethod>();
    public ICollection<MfaRecoveryCode> MfaRecoveryCodes { get; private set; } = new List<MfaRecoveryCode>();
    public ICollection<SecurityAuditLog> SecurityAuditLogs { get; private set; } = new List<SecurityAuditLog>();

    private Account() { }

    private Account(AccountId id, Username username, EmailAddress? email, PasswordHash? passwordHash) : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        IsEmailConfirmed = false;
        // IsActive = true;
        // IsDeleted = false;
        // CreatedAt = DateTime.UtcNow;
        // CreatedBy = "System";

        RaiseDomainEvent(new AccountCreatedDomainEvent(Id, Username.Value, Email?.Value));
    }

    /// <summary>
    /// Creates a new account instance with validated domain primitive value objects.
    /// </summary>
    /// <param name="rawUsername">The raw username string.</param>
    /// <param name="rawEmail">The optional raw email string.</param>
    /// <param name="rawPasswordHash">The optional hashed password string.</param>
    /// <returns>A result containing the created <see cref="Account"/> or a domain validation error.</returns>
    public static Result<Account> Create(
        string rawUsername,
        string? rawEmail = null,
        string? rawPasswordHash = null)
    {
        var usernameResult = Username.Create(rawUsername);
        if (usernameResult.IsFailure)
        {
            return Result.Failure<Account>(usernameResult.Error);
        }

        EmailAddress? email = null;
        if (!string.IsNullOrWhiteSpace(rawEmail))
        {
            var emailResult = EmailAddress.Create(rawEmail);
            if (emailResult.IsFailure)
            {
                return Result.Failure<Account>(emailResult.Error);
            }
            email = emailResult.Value;
        }

        PasswordHash? passwordHash = null;
        if (!string.IsNullOrWhiteSpace(rawPasswordHash))
        {
            var hashResult = PasswordHash.Create(rawPasswordHash);
            if (hashResult.IsFailure)
            {
                return Result.Failure<Account>(hashResult.Error);
            }
            passwordHash = hashResult.Value;
        }

        return new Account(AccountId.New(), usernameResult.Value, email, passwordHash);
    }

    /// <summary>
    /// Attaches or updates the email address associated with the account.
    /// </summary>
    /// <param name="rawEmail">The raw email string to attach.</param>
    /// <returns>A result indicating success or a domain validation error.</returns>
    public Result AttachEmail(string rawEmail)
    {
        var emailResult = EmailAddress.Create(rawEmail);
        if (emailResult.IsFailure)
        {
            return Result.Failure(emailResult.Error);
        }

        Email = emailResult.Value;
        IsEmailConfirmed = false;
        // ModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Confirms the current attached email address.
    /// </summary>
    /// <returns>A result indicating success or a validation failure if no email is attached.</returns>
    public Result ConfirmEmail()
    {
        if (Email is null)
        {
            return Error.Validation("Account.NoEmail", "Cannot confirm email because no email address is attached.");
        }

        if (IsEmailConfirmed)
        {
            return Result.Success();
        }

        IsEmailConfirmed = true;
        // ModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EmailConfirmedDomainEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Updates the password hash and revokes all active refresh tokens for security.
    /// </summary>
    /// <param name="rawNewPasswordHash">The raw new hashed password string.</param>
    /// <param name="updatedBy">The identity performing the password change.</param>
    /// <returns>A result indicating success or a domain validation error.</returns>
    public Result UpdatePassword(string rawNewPasswordHash, string updatedBy = "System")
    {
        var hashResult = PasswordHash.Create(rawNewPasswordHash);
        if (hashResult.IsFailure)
        {
            return Result.Failure(hashResult.Error);
        }

        PasswordHash = hashResult.Value;
        // ModifiedAt = DateTime.UtcNow;
        // ModifiedBy = updatedBy;

        RevokeAllRefreshTokens();

        RaiseDomainEvent(new PasswordChangedDomainEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Deactivates the account and revokes all active session tokens.
    /// </summary>
    /// <param name="reasonBy">The actor deactivating the account.</param>
    public void Deactivate(string reasonBy = "System")
    {
        IsActive = false;
        // ModifiedAt = DateTime.UtcNow;
        // ModifiedBy = reasonBy;
        RevokeAllRefreshTokens();
    }

    /// <summary>
    /// Performs a soft delete on the account and revokes all active session tokens.
    /// </summary>
    /// <param name="deletedBy">The actor performing the soft deletion.</param>
    public void SoftDelete(string deletedBy = "System")
    {
        if (IsDeleted) return;

        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = DateTime.UtcNow;
        // ModifiedAt = DateTime.UtcNow;
        // ModifiedBy = deletedBy;
        RevokeAllRefreshTokens();
    }
}