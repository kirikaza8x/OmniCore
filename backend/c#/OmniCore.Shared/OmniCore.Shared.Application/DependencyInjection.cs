namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using FluentValidation;
using OmniCore.Shared.Application.Behaviors;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Shared Application services, MediatR handlers, AutoMapper profiles, FluentValidation rules, and behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="moduleAssemblies">Additional domain/feature assemblies containing handlers or validators.</param>
    public static IServiceCollection AddSharedApplication(
        this IServiceCollection services,
        params Assembly[] moduleAssemblies)
    {
        var assembliesToRegister = new List<Assembly>
        {
            typeof(DependencyInjection).Assembly
        };

        if (moduleAssemblies is { Length: > 0 })
        {
            assembliesToRegister.AddRange(moduleAssemblies);
        }

        Assembly[] assembliesArray = assembliesToRegister.ToArray();

        // 1. Register MediatR with Pipeline Behaviors
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(assembliesArray);

            // Behavior Execution Pipeline (Outer -> Inner):
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));

        });

        // 2. Register AutoMapper
        services.AddAutoMapper(cfg => { }, assembliesArray);

        // 3. Register FluentValidation Validators (including internal validator classes)
        services.AddValidatorsFromAssemblies(assembliesArray, includeInternalTypes: true);

        return services;
    }
}