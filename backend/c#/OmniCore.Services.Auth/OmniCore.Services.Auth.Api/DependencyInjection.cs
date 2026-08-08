namespace OmniCore.Services.Auth.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Delegate presentation setup (Carter, Auth, Cors, RateLimiting, Swagger) to Shared.Api
        services.AddApi(
            moduleAssemblies: new[] { typeof(DependencyInjection).Assembly },
            configuration: configuration,
            apiTitle: "OmniCore Auth API");
        // 2. Local presentation services
        services.AddHttpContextAccessor();

        return services;
    }
}