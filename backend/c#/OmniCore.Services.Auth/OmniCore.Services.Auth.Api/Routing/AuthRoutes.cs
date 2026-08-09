namespace OmniCore.Services.Auth.Api.Routing;

using OmniCore.Shared.Api.Routing;

public sealed class AuthRoutes : BaseRouteModule
{
    // Sets the base prefix to /api/v1/auth
    protected override string ResourceName => "auth";

    // Service-specific sub-paths
    public string Register => "register";
    public string Login => "login";
    public string RefreshToken => "refresh-token";
    public string Logout => "logout";

    public string Me => "me";
}