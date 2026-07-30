namespace Microsoft.Extensions.DependencyInjection;

using OmniCore.Shared.Infrastructure.Configs;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Binds a configuration class to appsettings.json, validates Data Annotations, and enforces startup validation.
    /// </summary>
    public static IServiceCollection AddConfig<TConfig>(this IServiceCollection services)
        where TConfig : ConfigBase, new()
    {
        var tempInstance = new TConfig();

        services.AddOptions<TConfig>()
            .BindConfiguration(tempInstance.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}