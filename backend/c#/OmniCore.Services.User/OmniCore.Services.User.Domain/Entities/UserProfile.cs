// Entities/UserProfile.cs
namespace OmniCore.Services.User.Domain.Entities;

using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Events;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.ValueObjects;

public class UserProfile : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletable
{
    public Username Username { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }

    public UserSettings Settings { get; private set; } = null!;

    private readonly List<UserAddress> _addresses = [];
    public IReadOnlyCollection<UserAddress> Addresses => _addresses.AsReadOnly();

    private readonly List<UserTenantProfile> _tenantProfiles = [];
    public IReadOnlyCollection<UserTenantProfile> TenantProfiles => _tenantProfiles.AsReadOnly();

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public DateTime? CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    private UserProfile() { }

    private UserProfile(
        UserId id,
        Username username,
        EmailAddress email,
        string? firstName,
        string? lastName) : base(id)
    {
        Username = username;
        Email = email;
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        DisplayName = ComputeDisplayName(firstName, lastName, username.Value);
        IsActive = true;
        Settings = UserSettings.CreateDefault(id);

        RaiseDomainEvent(new UserProfileCreatedDomainEvent(Id, Username.Value, Email.Value));
    }

    public static Result<UserProfile> Create(
        Guid rawUserId,
        string rawUsername,
        string rawEmail,
        string? firstName = null,
        string? lastName = null)
    {
        var usernameResult = Username.Create(rawUsername);
        if (usernameResult.IsFailure)
        {
            return Result.Failure<UserProfile>(usernameResult.Error);
        }

        var emailResult = EmailAddress.Create(rawEmail);
        if (emailResult.IsFailure)
        {
            return Result.Failure<UserProfile>(emailResult.Error);
        }

        var userId = UserId.From(rawUserId);

        return new UserProfile(
            userId,
            usernameResult.Value,
            emailResult.Value,
            firstName,
            lastName);
    }

    public Result UpdatePersonalInformation(string? firstName, string? lastName, string? bio, string? phoneNumber)
    {
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        Bio = bio?.Trim();
        PhoneNumber = phoneNumber?.Trim();
        DisplayName = ComputeDisplayName(FirstName, LastName, Username.Value);

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id));
        return Result.Success();
    }

    public Result UpdateAvatar(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return Result.Failure(UserProfileErrors.AvatarUrlEmpty);
        }

        AvatarUrl = avatarUrl.Trim();
        RaiseDomainEvent(new UserAvatarUpdatedDomainEvent(Id, AvatarUrl));
        return Result.Success();
    }

    public void AddAddress(UserAddress address)
    {
        if (address.IsPrimary)
        {
            foreach (var existingAddress in _addresses)
            {
                existingAddress.SetPrimary(false);
            }
        }

        _addresses.Add(address);
    }

    public void SoftDelete(string deletedBy = "System")
    {
        if (IsDeleted) return;

        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = DateTime.UtcNow;
    }

    private static string ComputeDisplayName(string? firstName, string? lastName, string fallbackUsername)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? fallbackUsername : fullName;
    }
}