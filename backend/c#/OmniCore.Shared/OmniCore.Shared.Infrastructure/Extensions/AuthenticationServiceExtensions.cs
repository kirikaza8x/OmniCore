namespace Microsoft.Extensions.DependencyInjection;

using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Infrastructure.Services.Authentication;

/// <summary>
/// Extension methods for configuring identity context accessor and device detection services.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IHttpContextAccessor"/>, <see cref="IDeviceDetectionService"/>, and <see cref="ICurrentUserService"/> into DI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddSingleton<IDeviceDetectionService, DeviceDetectionService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}