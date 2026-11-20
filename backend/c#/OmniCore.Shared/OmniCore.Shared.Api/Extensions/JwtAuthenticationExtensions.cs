namespace OmniCore.Shared.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Shared.Api.Results;
using OmniCore.Shared.Infrastructure.Configs.Security;
using OmniCore.Shared.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        // 1. Configure JWT Authentication & Custom ProblemDetails Events
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Override default 401 / 403 responses with your Domain Result pattern
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    var error = Error.Unauthorized("Authentication.Failed", "You are not authenticated.");
                    var result = Result.Failure(error);

                    await CustomResults.Problem(result, context.HttpContext)
                        .ExecuteAsync(context.HttpContext);
                },
                OnForbidden = async context =>
                {
                    var error = Error.Forbidden("Authorization.Failed", "You do not have permission to perform this action.");
                    var result = Result.Failure(error);

                    await CustomResults.Problem(result, context.HttpContext)
                        .ExecuteAsync(context.HttpContext);
                }
            };
        });

        // 2. Bind JwtConfig deferred via DI Options
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtConfig>, JwtKeySet>((options, jwtConfigs, keySet) =>
            {
                var jwtSettings = jwtConfigs.Value;

                // Keys come from the key set: local public keys (Auth) or the cached JWKS (other services), plus the
                // legacy HS256 secret while it is configured. Only the algorithms the key set lists are accepted.
                options.TokenValidationParameters.ValidAlgorithms = keySet.ValidAlgorithms;
                options.TokenValidationParameters.IssuerSigningKeyResolver =
                    (token, securityToken, kid, parameters) => keySet.ResolveKeys(kid);
                options.TokenValidationParameters.ValidIssuer = jwtSettings.Issuer;
                options.TokenValidationParameters.ValidAudience = jwtSettings.Audience;
            });

        // The key set is shared by validation (here) and by the Auth service when it signs tokens.
        services.AddSingleton<JwtKeySet>();

        // 3. Register Authorization Framework
        services.AddAuthorization();

        return services;
    }
}