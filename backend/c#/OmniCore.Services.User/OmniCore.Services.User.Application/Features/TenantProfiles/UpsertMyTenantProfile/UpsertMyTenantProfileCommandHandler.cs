namespace OmniCore.Services.User.Application.Features.TenantProfiles.UpsertMyTenantProfile;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpsertMyTenantProfileCommandHandler(
    IUserTenantProfileRepository tenantProfiles,
    IUserProfileRepository profiles,
    ICurrentUserService currentUser,
    ITenantContext tenantContext,
    IDateTimeProvider clock)
    : ICommandHandler<UpsertMyTenantProfileCommand, TenantProfileResponse>
{
    public async Task<Result<TenantProfileResponse>> Handle(UpsertMyTenantProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<TenantProfileResponse>(userId.Error);

        // TenantBehavior already refused a call without a tenant.
        var tenantId = tenantContext.TenantId!.Value;

        var existing = await tenantProfiles.GetForUserAsync(userId.Value, cancellationToken);
        if (existing is not null)
        {
            var updated = existing.Update(request.GamerTag, request.CustomAvatarUrl);
            return updated.IsFailure ? Result.Failure<TenantProfileResponse>(updated.Error) : existing.ToResponse();
        }

        // A tenant profile hangs off the person's profile (foreign key): that profile must exist.
        if (!await profiles.ExistsAsync(userId.Value, cancellationToken))
        {
            return Result.Failure<TenantProfileResponse>(UserProfileErrors.NotFound);
        }

        var created = UserTenantProfile.Create(
            tenantId, userId.Value, request.GamerTag, request.CustomAvatarUrl, clock.UtcNow);
        if (created.IsFailure) return Result.Failure<TenantProfileResponse>(created.Error);

        tenantProfiles.Add(created.Value);
        return created.Value.ToResponse();
    }
}
