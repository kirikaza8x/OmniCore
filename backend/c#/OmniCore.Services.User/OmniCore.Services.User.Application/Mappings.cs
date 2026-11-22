namespace OmniCore.Services.User.Application;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Entities;

public static class MappingExtensions
{
    public static UserProfileResponse ToResponse(this UserProfile profile) => new(
        profile.Id.Value,
        profile.Username.Value,
        profile.Email?.Value,
        profile.FirstName,
        profile.LastName,
        profile.DisplayName,
        profile.Bio,
        profile.AvatarUrl,
        profile.PhoneNumber,
        profile.IsActive,
        profile.Settings.ToResponse(),
        profile.Addresses.Select(a => a.ToResponse()).ToList());

    public static PublicProfileResponse ToPublicResponse(this UserProfile profile) => new(
        profile.Id.Value,
        profile.DisplayName ?? profile.Username.Value,
        profile.AvatarUrl,
        profile.Bio);

    public static UserSettingsResponse ToResponse(this UserSettings settings) => new(
        settings.Theme,
        settings.Language,
        settings.EmailNotificationsEnabled,
        settings.PushNotificationsEnabled);

    public static UserAddressResponse ToResponse(this UserAddress address) => new(
        address.Id.Value,
        address.AddressLine1,
        address.AddressLine2,
        address.City,
        address.State,
        address.Country,
        address.ZipCode,
        address.IsPrimary);
}
