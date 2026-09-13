// Features/UserSettings/Commands/UpdateUserSettings/UpdateUserSettingsCommand.cs
namespace OmniCore.Services.User.Application.Features.UserSettings.Commands.UpdateUserSettings;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record UpdateUserSettingsCommand(
    Guid UserId,
    string Theme,
    string Language,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled) : ICommand<UserSettingsResponse>;