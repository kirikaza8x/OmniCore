namespace Microsoft.Extensions.DependencyInjection;

using OmniCore.Shared.Infrastructure.Configs;

/// <summary>
/// Extension methods for binding, validating, and registering strong-typed configuration objects.
/// </summary>
public static class OmniCoreConfigurationExtensions
{
    /// <summary>
    /// Binds a configuration class deriving from <see cref="ConfigBase"/> to appsettings.json, validates Data Annotations, and enforces startup validation.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type inheriting from <see cref="ConfigBase"/>.</typeparam>
    /// <param name="services">The service collection to add options to.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddConfig<TConfig>(this IServiceCollection services)
        where TConfig : ConfigBase, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var tempInstance = new TConfig();

        services.AddOptions<TConfig>()
            .BindConfiguration(tempInstance.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}