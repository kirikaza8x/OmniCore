using OmniCore.Shared.Infrastructure.Security;

namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

/// <summary>
/// Publishes the public signing keys (ADR 0003 step 4) so any service, in any language, can verify access tokens
/// without being able to create them. Anonymous by design: it contains public keys only.
/// </summary>
public sealed class JwksEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/jwks.json", (JwtKeySet keys, HttpContext http) =>
            {
                http.Response.Headers.CacheControl = "public, max-age=300";
                return Results.Json(keys.BuildJwks());
            })
           .AllowAnonymous()
           .WithTags("Auth")
           .WithName("Jwks")
           .ExcludeFromDescription();
    }
}
