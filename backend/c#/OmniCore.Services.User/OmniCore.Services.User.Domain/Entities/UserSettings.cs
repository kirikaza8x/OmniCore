namespace OmniCore.Services.User.Domain.Entities;

using System.Text.RegularExpressions;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class UserSettings : Entity<UserSettingsId>
{
    public static readonly string[] Themes = ["Light", "Dark", "System"];

    private static readonly Regex LanguageRegex = new(@"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})?$", RegexOptions.Compiled);

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

    internal static UserSettings CreateDefault(UserId userId) => new(UserSettingsId.New(), userId);

    /// <summary>Changed only through <see cref="UserProfile.UpdateSettings"/> so the aggregate stays in charge.</summary>
    internal Result UpdatePreferences(string theme, string language, bool emailNotifications, bool pushNotifications)
    {
        var knownTheme = Themes.FirstOrDefault(t => string.Equals(t, theme?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (knownTheme is null)
        {
            return Result.Failure(UserProfileErrors.ThemeInvalid);
        }

        var lang = language?.Trim() ?? string.Empty;
        if (!LanguageRegex.IsMatch(lang))
        {
            return Result.Failure(UserProfileErrors.LanguageInvalid);
        }

        Theme = knownTheme;
        Language = lang;
        EmailNotificationsEnabled = emailNotifications;
        PushNotificationsEnabled = pushNotifications;
        return Result.Success();
    }
}
