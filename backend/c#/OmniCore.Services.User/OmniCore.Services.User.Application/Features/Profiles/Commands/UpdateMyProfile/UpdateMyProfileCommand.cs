namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMyProfile;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

/// <summary>Replaces the caller's names, bio and phone. The caller comes from the token.</summary>
public record UpdateMyProfileCommand(string? FirstName, string? LastName, string? Bio, string? PhoneNumber)
    : ICommand<UserProfileResponse>;
