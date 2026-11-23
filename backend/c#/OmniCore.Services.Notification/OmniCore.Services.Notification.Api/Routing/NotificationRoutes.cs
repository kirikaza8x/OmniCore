namespace OmniCore.Services.Notification.Api.Routing;

using OmniCore.Shared.Api.Routing;

/// <summary>Gives <c>/api/v1/notifications</c>.</summary>
public sealed class NotificationRoutes : BaseRouteModule
{
    protected override string? ServiceName => "notifications";
    protected override string? ResourceName => null;

    public string Test => "test";
}
