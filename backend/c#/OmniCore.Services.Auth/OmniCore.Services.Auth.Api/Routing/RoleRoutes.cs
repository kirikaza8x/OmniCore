namespace OmniCore.Services.Auth.Api.Routing;

using OmniCore.Shared.Api.Routing;

public sealed class RoleRoutes : BaseRouteModule
{
    protected override string? ServiceName => "auth";
    protected override string? ResourceName => "roles";
    public string AssignRole => "assign";
    public string RemoveRole => "remove";
}