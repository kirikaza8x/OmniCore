namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.ValueObjects;

public class Account : AggregateRoot<AccountId>, IAuditableEntity, ISoftDeletable
{
    public Username Username { get; private set; } = null!;
    public EmailAddress? Email { get; private set; }
    public PasswordHash? PasswordHash { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime? CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    // Navigation Collections
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<ExternalLogin> ExternalLogins { get; private set; } = new List<ExternalLogin>();
    public ICollection<SecurityToken> SecurityTokens { get; private set; } = new List<SecurityToken>();
    public ICollection<MfaMethod> MfaMethods { get; private set; } = new List<MfaMethod>();
    public ICollection<MfaRecoveryCode> MfaRecoveryCodes { get; private set; } = new List<MfaRecoveryCode>();
    public ICollection<SecurityAuditLog> SecurityAuditLogs { get; private set; } = new List<SecurityAuditLog>();
    public ICollection<AccountRole> AccountRoles { get; private set; } = new List<AccountRole>();

    private Account() { }

    private Account(AccountId id, Username username, EmailAddress? email, PasswordHash? passwordHash) : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        IsEmailConfirmed = false;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = "System";

        RaiseDomainEvent(new AccountCreatedDomainEvent(Id, Username.Value, Email?.Value));
    }

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

    public Result AttachEmail(string rawEmail)
    {
        var emailResult = EmailAddress.Create(rawEmail);
        if (emailResult.IsFailure)
        {
            return Result.Failure(emailResult.Error);
        }

        Email = emailResult.Value;
        IsEmailConfirmed = false;
        ModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

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
        ModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EmailConfirmedDomainEvent(Id));
        return Result.Success();
    }

    public Result UpdatePassword(string rawNewPasswordHash, string updatedBy = "System")
    {
        var hashResult = PasswordHash.Create(rawNewPasswordHash);
        if (hashResult.IsFailure)
        {
            return Result.Failure(hashResult.Error);
        }

        PasswordHash = hashResult.Value;
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = updatedBy;
        RaiseDomainEvent(new PasswordChangedDomainEvent(Id));
        return Result.Success();
    }

    public void Deactivate(string reasonBy = "System")
    {
        IsActive = false;
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = reasonBy;
    }
}