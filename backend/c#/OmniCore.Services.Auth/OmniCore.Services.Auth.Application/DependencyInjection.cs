namespace OmniCore.Services.Auth.Application;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddSharedApplication(ApplicationAssemblyReference.Assembly);
        return services;
    }
}