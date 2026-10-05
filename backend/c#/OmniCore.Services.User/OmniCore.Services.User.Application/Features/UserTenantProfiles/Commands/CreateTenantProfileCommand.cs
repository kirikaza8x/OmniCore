// Features/UserTenantProfiles/Commands/CreateTenantProfile/CreateTenantProfileCommand.cs
namespace OmniCore.Services.User.Application.Features.UserTenantProfiles.Commands.CreateTenantProfile;

using OmniCore.Services.User.Application.Features.UserTenantProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record CreateTenantProfileCommand(
    string TenantId,
    Guid UserId,
    string GamerTag,
    string? CustomAvatarUrl = null) : ICommand<UserTenantProfileResponse>;