namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMySettings;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateMySettingsCommandHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : ICommandHandler<UpdateMySettingsCommand, UserSettingsResponse>
{
    public async Task<Result<UserSettingsResponse>> Handle(UpdateMySettingsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<UserSettingsResponse>(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure<UserSettingsResponse>(UserProfileErrors.NotFound);

        var updated = profile.UpdateSettings(
            request.Theme, request.Language, request.EmailNotificationsEnabled, request.PushNotificationsEnabled);
        if (updated.IsFailure) return Result.Failure<UserSettingsResponse>(updated.Error);

        return profile.Settings.ToResponse();
    }
}
