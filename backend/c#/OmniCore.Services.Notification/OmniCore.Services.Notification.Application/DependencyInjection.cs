namespace OmniCore.Services.Notification.Application;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddSharedApplication(ApplicationAssemblyReference.Assembly);
        return services;
    }
}
