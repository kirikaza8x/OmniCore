namespace OmniCore.Services.User.Application.DTOs;

public record UserSettingsResponse(
    string Theme,
    string Language,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled);

public record UserAddressResponse(
    Guid Id,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string Country,
    string ZipCode,
    bool IsPrimary);

/// <summary>The owner's (or an admin's) view: everything we store.</summary>
public record UserProfileResponse(
    Guid Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? PhoneNumber,
    bool IsActive,
    UserSettingsResponse Settings,
    List<UserAddressResponse> Addresses);

/// <summary>What any signed-in user may see of someone else: no e-mail, phone, addresses or settings.</summary>
public record PublicProfileResponse(
    Guid Id,
    string DisplayName,
    string? AvatarUrl,
    string? Bio);

// Request bodies of the API (the user id is never part of them: it comes from the token).
public record UpdateMyProfileRequest(string? FirstName, string? LastName, string? Bio, string? PhoneNumber);

public record UpdateMySettingsRequest(
    string Theme,
    string Language,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled);

public record AddressRequest(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string Country,
    string? ZipCode,
    bool IsPrimary);
