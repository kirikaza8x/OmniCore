namespace OmniCore.Services.Auth.Api.Routing;

using OmniCore.Shared.Api.Routing;

public sealed class AuthRoutes : BaseRouteModule
{
    // Sets the service level prefix (/api/v1/auth)
    protected override string? ServiceName => "auth";

    // Standard auth actions operate at the service root level, so ResourceName is null
    protected override string? ResourceName => null;

    // Sub-paths
    public string Register => "register";
    public string Login => "login";
    public string RefreshToken => "refresh-token";
    public string Logout => "logout";

    public string Me => "me";
}