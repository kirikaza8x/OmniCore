namespace OmniCore.Services.Notification.Application;

using Microsoft.Extensions.DependencyInjection;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Application.Templates;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddSharedApplication(ApplicationAssemblyReference.Assembly);

        // The default templates are pure logic, so they live here; replace this registration to load templates elsewhere.
        services.AddSingleton<ITemplateRenderer, BuiltInTemplateRenderer>();
        return services;
    }
}
