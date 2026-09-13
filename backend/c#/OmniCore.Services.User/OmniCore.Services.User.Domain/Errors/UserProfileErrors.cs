// Errors/UserProfileErrors.cs
namespace OmniCore.Services.User.Domain.Errors;

using OmniCore.Shared.Domain.Abstractions;

public static class UserProfileErrors
{
    public static readonly Error NotFound = 
        Error.NotFound("UserProfile.NotFound", "The requested user profile was not found.");

    public static readonly Error UsernameAlreadyExists = 
        Error.Conflict("UserProfile.UsernameConflict", "The specified username is already in use.");

    public static readonly Error EmailAlreadyExists = 
        Error.Conflict("UserProfile.EmailConflict", "The specified email address is already in use.");

    public static readonly Error AddressNotFound = 
        Error.NotFound("UserAddress.NotFound", "The specified address was not found on this profile.");

    public static readonly Error AvatarUrlEmpty = 
        Error.Validation("UserProfile.AvatarUrlEmpty", "Avatar URL cannot be empty.");
}