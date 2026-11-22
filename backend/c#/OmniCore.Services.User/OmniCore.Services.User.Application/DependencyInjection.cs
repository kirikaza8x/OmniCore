namespace OmniCore.Services.User.Application;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        services.AddSharedApplication(ApplicationAssemblyReference.Assembly);
        return services;
    }
}
