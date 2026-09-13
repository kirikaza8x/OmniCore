// Features/UserProfiles/DTOs/UserProfileDTOs.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.DTOs;

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

public record UserProfileResponse(
    Guid Id, 
    string Username, 
    string Email, 
    string? FirstName, 
    string? LastName, 
    string? DisplayName, 
    string? Bio, 
    string? AvatarUrl, 
    string? PhoneNumber, 
    bool IsActive,
    UserSettingsResponse? Settings,
    List<UserAddressResponse> Addresses);

public record CreateUserProfileRequest(
    Guid UserId, 
    string Username, 
    string Email, 
    string? FirstName, 
    string? LastName);

public record UpdateUserProfileRequest(
    string? FirstName, 
    string? LastName, 
    string? Bio, 
    string? PhoneNumber);