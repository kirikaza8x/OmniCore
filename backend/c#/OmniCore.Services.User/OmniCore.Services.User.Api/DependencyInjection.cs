namespace OmniCore.Services.User.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddUserApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApi(
            moduleAssemblies: [typeof(DependencyInjection).Assembly],
            configuration: configuration,
            apiTitle: "OmniCore User API");

        services.AddHttpContextAccessor();
        return services;
    }
}
