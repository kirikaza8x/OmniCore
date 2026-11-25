namespace OmniCore.Services.User.Domain.Entities;

using System.Text.RegularExpressions;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Events;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.ValueObjects;

/// <summary>
/// What we know about a person beyond their login. The id is the Auth account id (ADR 0004).
/// User name and e-mail are display copies of Auth's data.
/// </summary>
public class UserProfile : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletable
{
    public const int MaxAddresses = 10;
    public const int MaxNameLength = 100;
    public const int MaxBioLength = 500;
    public const int MaxPhoneLength = 32;

    private static readonly Regex PhoneRegex = new(@"^[0-9+\-() ]+$", RegexOptions.Compiled);

    public Username Username { get; private set; } = null!;
    public EmailAddress? Email { get; private set; }
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

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public DateTime? CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    private UserProfile() { }

    private UserProfile(UserId id, Username username, EmailAddress? email, string? firstName, string? lastName) : base(id)
    {
        Username = username;
        Email = email;
        FirstName = NullIfBlank(firstName);
        LastName = NullIfBlank(lastName);
        DisplayName = ComputeDisplayName(FirstName, LastName, username.Value);
        IsActive = true;
        Settings = UserSettings.CreateDefault(id);

        RaiseDomainEvent(new UserProfileCreatedDomainEvent(Id, Username.Value, Email?.Value));
    }

    /// <param name="accountId">The Auth account id. Never a freshly generated id for an existing account.</param>
    /// <param name="email">Optional: Auth's e-mail is optional too. A blank value means none.</param>
    public static Result<UserProfile> Create(
        Guid accountId, string username, string? email, string? firstName = null, string? lastName = null)
    {
        if (accountId == Guid.Empty)
        {
            return Result.Failure<UserProfile>(Error.Validation("UserProfile.IdEmpty", "The account id is required."));
        }

        var usernameResult = Username.Create(username);
        if (usernameResult.IsFailure) return Result.Failure<UserProfile>(usernameResult.Error);

        EmailAddress? emailValue = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = EmailAddress.Create(email);
            if (emailResult.IsFailure) return Result.Failure<UserProfile>(emailResult.Error);
            emailValue = emailResult.Value;
        }

        var nameCheck = CheckNames(firstName, lastName);
        if (nameCheck.IsFailure) return Result.Failure<UserProfile>(nameCheck.Error);

        return new UserProfile(UserId.From(accountId), usernameResult.Value, emailValue, firstName, lastName);
    }

    /// <summary>Replaces all four fields (a blank value clears it), as a PUT does.</summary>
    public Result UpdatePersonalInformation(string? firstName, string? lastName, string? bio, string? phoneNumber)
    {
        var nameCheck = CheckNames(firstName, lastName);
        if (nameCheck.IsFailure) return nameCheck;

        if (bio is not null && bio.Trim().Length > MaxBioLength)
            return Result.Failure(UserProfileErrors.TooLong("Bio", MaxBioLength));

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phone = phoneNumber.Trim();
            if (phone.Length > MaxPhoneLength) return Result.Failure(UserProfileErrors.TooLong("PhoneNumber", MaxPhoneLength));
            if (!PhoneRegex.IsMatch(phone)) return Result.Failure(UserProfileErrors.PhoneInvalid);
        }

        FirstName = NullIfBlank(firstName);
        LastName = NullIfBlank(lastName);
        Bio = NullIfBlank(bio);
        PhoneNumber = NullIfBlank(phoneNumber);
        DisplayName = ComputeDisplayName(FirstName, LastName, Username.Value);

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id));
        return Result.Success();
    }

    public Result UpdateAvatar(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl)) return Result.Failure(UserProfileErrors.AvatarUrlEmpty);
        if (avatarUrl.Trim().Length > 2048) return Result.Failure(UserProfileErrors.TooLong("AvatarUrl", 2048));

        AvatarUrl = avatarUrl.Trim();
        RaiseDomainEvent(new UserAvatarUpdatedDomainEvent(Id, AvatarUrl));
        return Result.Success();
    }

    public Result UpdateSettings(string theme, string language, bool emailNotifications, bool pushNotifications) =>
        Settings.UpdatePreferences(theme, language, emailNotifications, pushNotifications);

    /// <summary>The first address becomes primary. Asking for a primary one clears the flag on the others.</summary>
    public Result<UserAddress> AddAddress(
        string addressLine1, string? addressLine2, string city, string state, string country, string zipCode, bool makePrimary)
    {
        if (_addresses.Count >= MaxAddresses) return Result.Failure<UserAddress>(UserProfileErrors.TooManyAddresses);

        var created = UserAddress.Create(Id, addressLine1, addressLine2, city, state, country, zipCode);
        if (created.IsFailure) return created;

        var address = created.Value;
        _addresses.Add(address);
        if (makePrimary || _addresses.Count == 1) PromoteToPrimary(address);

        return address;
    }

    public Result<UserAddress> UpdateAddress(
        UserAddressId addressId, string addressLine1, string? addressLine2, string city, string state, string country,
        string zipCode, bool makePrimary)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Failure<UserAddress>(UserProfileErrors.AddressNotFound);

        var result = address.Update(addressLine1, addressLine2, city, state, country, zipCode);
        if (result.IsFailure) return Result.Failure<UserAddress>(result.Error);

        if (makePrimary) PromoteToPrimary(address);
        return address;
    }

    /// <summary>Removing the primary address makes the oldest remaining one primary.</summary>
    public Result RemoveAddress(UserAddressId addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return Result.Failure(UserProfileErrors.AddressNotFound);

        _addresses.Remove(address);
        if (address.IsPrimary && _addresses.Count > 0) PromoteToPrimary(_addresses[0]);

        return Result.Success();
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new UserProfileDeletedDomainEvent(Id));
    }

    private void PromoteToPrimary(UserAddress address)
    {
        foreach (var existing in _addresses) existing.SetPrimary(ReferenceEquals(existing, address));
    }

    private static Result CheckNames(string? firstName, string? lastName)
    {
        if (firstName is not null && firstName.Trim().Length > MaxNameLength)
            return Result.Failure(UserProfileErrors.TooLong("FirstName", MaxNameLength));
        if (lastName is not null && lastName.Trim().Length > MaxNameLength)
            return Result.Failure(UserProfileErrors.TooLong("LastName", MaxNameLength));
        return Result.Success();
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeDisplayName(string? firstName, string? lastName, string fallbackUsername)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? fallbackUsername : fullName;
    }
}
