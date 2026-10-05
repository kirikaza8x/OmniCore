// Features/UserTenantProfiles/Commands/CreateTenantProfile/CreateTenantProfileCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserTenantProfiles.Commands.CreateTenantProfile;

using OmniCore.Services.User.Application.Features.UserTenantProfiles.DTOs;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public sealed class CreateTenantProfileCommandHandler(
    IUserProfileRepository userProfileRepository) : ICommandHandler<CreateTenantProfileCommand, UserTenantProfileResponse>
{
    public async Task<Result<UserTenantProfileResponse>> Handle(
        CreateTenantProfileCommand request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserTenantProfileResponse>(UserProfileErrors.NotFound);
        }

        var tenantId = TenantId.From(request.TenantId);
        var tenantProfileResult = UserTenantProfile.Create(
            tenantId,
            userId,
            request.GamerTag,
            request.CustomAvatarUrl);

        if (tenantProfileResult.IsFailure)
        {
            return Result.Failure<UserTenantProfileResponse>(tenantProfileResult.Error);
        }

        var tenantProfile = tenantProfileResult.Value;

        return Result.Success(new UserTenantProfileResponse(
            tenantProfile.Id.Value,
            tenantProfile.TenantId.Value,
            tenantProfile.UserId.Value,
            tenantProfile.GamerTag,
            tenantProfile.CustomAvatarUrl,
            tenantProfile.JoinedAtUtc));
    }
}