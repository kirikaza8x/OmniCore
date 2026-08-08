namespace OmniCore.Shared.Api.Extensions;

using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OmniCore.Shared.Api.Exceptions;
using OmniCore.Shared.Infrastructure.Configs;
using OmniCore.Shared.Infrastructure.Hubs;

public static class ApiExtensions
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        Assembly[] moduleAssemblies,
        IConfiguration configuration,
        string apiTitle = "OmniCore API")
    {
        services.AddCarterWithAssemblies(moduleAssemblies);
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddSignalR();
        
        // Registers Swagger DI services
        services.AddSwaggerDocumentation(title: apiTitle); 

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        ConfigureRateLimiting(services, configuration);

        var corsConfig = new CorsConfig();
        var corsSection = configuration.GetSection("Cors");

        if (corsSection.Exists())
        {
            corsSection.Bind(corsConfig);
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (corsConfig.AllowAnyOrigin)
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else if (corsConfig.AllowedOrigins.Any())
                {
                    policy.WithOrigins(corsConfig.AllowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        return services;
    }

    public static WebApplication UseApi(
        this WebApplication app, 
        string apiTitle = "OmniCore API",
        bool? enableSwagger = null) 
    {
        app.UseExceptionHandler(); 

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Resolves from argument, or automatically falls back to appsettings/env config
        var isSwaggerEnabled = enableSwagger 
            ?? app.Configuration.GetValue<bool>("EnableSwagger", app.Environment.IsDevelopment());

        if (isSwaggerEnabled)
        {
            app.UseSwaggerDocumentation(title: apiTitle); 
        }

        app.MapHub<LogHub>("api/logHub");

        return app;
    }

    private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var sectionName = new RateLimitingConfig().SectionName;
        var rateLimitingConfig = configuration.GetSection(sectionName).Get<RateLimitingConfig>();

        if (rateLimitingConfig is null)
        {
            throw new InvalidOperationException($"Missing configuration section '{sectionName}'.");
        }

        if (string.IsNullOrWhiteSpace(rateLimitingConfig.GlobalPolicy))
        {
            throw new InvalidOperationException("RateLimiting:GlobalPolicy is required.");
        }

        if (!rateLimitingConfig.Policies.Any())
        {
            throw new InvalidOperationException("RateLimiting:Policies must contain at least one policy.");
        }

        if (!rateLimitingConfig.Policies.TryGetValue(rateLimitingConfig.GlobalPolicy, out var globalPolicyConfig))
        {
            throw new InvalidOperationException($"Global policy '{rateLimitingConfig.GlobalPolicy}' was not found in RateLimiting:Policies.");
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            foreach (var (policyName, policyConfig) in rateLimitingConfig.Policies)
            {
                options.AddPolicy(policyName, httpContext =>
                {
                    var partitionKey = BuildPartitionKey(httpContext, policyName);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = policyConfig.PermitLimit,
                            Window = TimeSpan.FromSeconds(policyConfig.WindowSeconds),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = policyConfig.QueueLimit,
                            AutoReplenishment = true
                        });
                });
            }

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = BuildPartitionKey(httpContext, rateLimitingConfig.GlobalPolicy);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPolicyConfig.PermitLimit,
                        Window = TimeSpan.FromSeconds(globalPolicyConfig.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = globalPolicyConfig.QueueLimit,
                        AutoReplenishment = true
                    });
            });
        });
    }

    private static string BuildPartitionKey(HttpContext httpContext, string policyName)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? httpContext.User.FindFirstValue("sub");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"{policyName}:user:{userId}";
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return $"{policyName}:ip:{ipAddress}";
    }
}