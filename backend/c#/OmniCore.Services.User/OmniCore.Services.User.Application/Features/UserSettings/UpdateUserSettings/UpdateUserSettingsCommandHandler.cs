// Features/UserSettings/Commands/UpdateUserSettings/UpdateUserSettingsCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserSettings.Commands.UpdateUserSettings;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Services.User.Application.Features.UserProfiles.Mappings;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.Specifications;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateUserSettingsCommandHandler(
    IUserProfileRepository userProfileRepository) : ICommandHandler<UpdateUserSettingsCommand, UserSettingsResponse>
{
    public async Task<Result<UserSettingsResponse>> Handle(
        UpdateUserSettingsCommand request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var spec = new UserProfileWithSettingsSpecification(userId);
        var profile = await userProfileRepository.GetWithSpecificationAsync(spec, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserSettingsResponse>(UserProfileErrors.NotFound);
        }

        profile.Settings.UpdatePreferences(
            request.Theme,
            request.Language,
            request.EmailNotificationsEnabled,
            request.PushNotificationsEnabled);

        userProfileRepository.Update(profile);

        return Result.Success(profile.Settings.ToResponse());
    }
}