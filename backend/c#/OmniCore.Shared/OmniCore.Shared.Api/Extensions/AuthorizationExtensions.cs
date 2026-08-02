namespace OmniCore.Shared.Api.Extensions;

using Microsoft.AspNetCore.Builder;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequireRoles(this RouteHandlerBuilder builder, params string[] roles)
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireRole(roles);
        });
    }
}