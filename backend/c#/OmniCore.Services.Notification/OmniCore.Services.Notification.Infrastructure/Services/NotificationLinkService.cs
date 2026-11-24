namespace OmniCore.Services.Notification.Infrastructure.Services;

using Microsoft.Extensions.Options;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Infrastructure.Configs;

public sealed class NotificationLinkService(IOptions<NotificationLinksConfig> options) : INotificationLinks
{
    public string ResetPassword(string token) => Build("reset-password", token);

    public string VerifyEmail(string token) => Build("verify-email", token);

    private string Build(string path, string token) =>
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
}
