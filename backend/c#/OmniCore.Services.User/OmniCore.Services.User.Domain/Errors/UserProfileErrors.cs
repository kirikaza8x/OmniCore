namespace OmniCore.Services.User.Domain.Errors;

using OmniCore.Shared.Domain.Abstractions;

public static class UserProfileErrors
{
    public static readonly Error NotFound =
        Error.NotFound("UserProfile.NotFound", "The requested user profile was not found.");

    public static readonly Error AddressNotFound =
        Error.NotFound("UserAddress.NotFound", "The specified address was not found on this profile.");

    public static readonly Error AvatarUrlEmpty =
        Error.Validation("UserProfile.AvatarUrlEmpty", "Avatar URL cannot be empty.");

    public static readonly Error TooManyAddresses =
        Error.Conflict("UserAddress.TooMany", $"A profile can have at most {Entities.UserProfile.MaxAddresses} addresses.");

    public static readonly Error Unauthenticated =
        Error.Unauthorized("UserProfile.Unauthenticated", "The caller is not signed in.");

    public static Error TooLong(string field, int max) =>
        Error.Validation($"UserProfile.{field}TooLong", $"{field} cannot exceed {max} characters.");

    public static readonly Error PhoneInvalid =
        Error.Validation("UserProfile.PhoneInvalid", "Phone number may contain only digits, spaces and + - ( ).");

    public static readonly Error ThemeInvalid =
        Error.Validation("UserSettings.ThemeInvalid", "Theme must be Light, Dark or System.");

    public static readonly Error LanguageInvalid =
        Error.Validation("UserSettings.LanguageInvalid", "Language must be a code such as en or en-US.");
}
