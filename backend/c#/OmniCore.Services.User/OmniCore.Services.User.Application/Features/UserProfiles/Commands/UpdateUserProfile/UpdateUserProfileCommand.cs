// Features/UserProfiles/Commands/UpdateUserProfile/UpdateUserProfileCommand.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UpdateUserProfile;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record UpdateUserProfileCommand(
    Guid UserId, 
    string? FirstName, 
    string? LastName, 
    string? Bio, 
    string? PhoneNumber) : ICommand<UserProfileResponse>;