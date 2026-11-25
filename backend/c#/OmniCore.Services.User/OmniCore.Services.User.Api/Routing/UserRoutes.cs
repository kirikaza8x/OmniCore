namespace OmniCore.Services.User.Api.Routing;

using OmniCore.Shared.Api.Routing;

/// <summary>Gives <c>/api/v1/users</c>. "me" is always the signed-in caller (the id comes from the token).</summary>
public sealed class UserRoutes : BaseRouteModule
{
    protected override string? ServiceName => "users";
    protected override string? ResourceName => null;

    public string Me => "me";
    public string MySettings => "me/settings";
    public string MyAvatar => "me/avatar";
    public string MyAddresses => "me/addresses";
    public string MyAddress => "me/addresses/{addressId:guid}";
    public string MyTenantProfile => "me/tenant-profile";
    public string PublicProfile => "{id:guid}";
    public string ProfileDetails => "{id:guid}/details";
}
