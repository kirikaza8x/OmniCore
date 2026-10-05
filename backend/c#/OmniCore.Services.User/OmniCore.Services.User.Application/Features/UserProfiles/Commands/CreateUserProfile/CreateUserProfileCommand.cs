// Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommand.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.CreateUserProfile;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record CreateUserProfileCommand(
    Guid UserId, 
    string Username, 
    string Email, 
    string? FirstName = null, 
    string? LastName = null) : ICommand<UserProfileResponse>;