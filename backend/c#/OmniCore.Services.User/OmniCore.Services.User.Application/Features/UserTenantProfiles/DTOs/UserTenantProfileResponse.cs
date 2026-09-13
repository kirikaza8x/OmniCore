// Features/UserTenantProfiles/DTOs/UserTenantProfileResponse.cs
namespace OmniCore.Services.User.Application.Features.UserTenantProfiles.DTOs;

public record UserTenantProfileResponse(
    Guid Id,
    string TenantId,
    Guid UserId,
    string GamerTag,
    string? CustomAvatarUrl,
    DateTime JoinedAtUtc);