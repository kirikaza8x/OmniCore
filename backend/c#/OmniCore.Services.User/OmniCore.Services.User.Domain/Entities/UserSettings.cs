// Entities/UserSettings.cs
namespace OmniCore.Services.User.Domain.Entities;

using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

public class UserSettings : Entity<UserSettingsId>
{
    public UserId UserId { get; private set; } = null!;
    public string Theme { get; private set; } = "Dark";
    public string Language { get; private set; } = "en-US";
    public bool EmailNotificationsEnabled { get; private set; } = true;
    public bool PushNotificationsEnabled { get; private set; } = true;

    public UserProfile UserProfile { get; private set; } = null!;

    private UserSettings() { }

    private UserSettings(UserSettingsId id, UserId userId) : base(id)
    {
        UserId = userId;
    }

    public static UserSettings CreateDefault(UserId userId)
    {
        return new UserSettings(UserSettingsId.New(), userId);
    }

    public void UpdatePreferences(string theme, string language, bool emailNotifications, bool pushNotifications)
    {
        Theme = theme;
        Language = language;
        EmailNotificationsEnabled = emailNotifications;
        PushNotificationsEnabled = pushNotifications;
    }
}