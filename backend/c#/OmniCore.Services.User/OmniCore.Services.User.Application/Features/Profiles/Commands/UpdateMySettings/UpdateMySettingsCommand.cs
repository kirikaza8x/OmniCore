namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMySettings;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record UpdateMySettingsCommand(
    string Theme,
    string Language,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled) : ICommand<UserSettingsResponse>;
