namespace OmniCore.Services.Notification.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApi(
            moduleAssemblies: [typeof(DependencyInjection).Assembly],
            configuration: configuration,
            apiTitle: "OmniCore Notification API");

        services.AddHttpContextAccessor();
        return services;
    }
}
