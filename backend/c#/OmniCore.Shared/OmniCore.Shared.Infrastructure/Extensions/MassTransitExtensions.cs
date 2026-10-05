namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Contracts.Events;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;
using DataValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitWithBroker<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies) where TDbContext : DbContext
    {
        return services.AddMassTransitWithBroker<TDbContext>(configuration, configureOutbox: null, assemblies);
    }

    public static IServiceCollection AddMassTransitWithBroker<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IEntityFrameworkOutboxConfigurator>? configureOutbox,
        params Assembly[] assemblies) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddConfig<MessageBrokerConfig>();
        RegisterIntegrationEventHandlers(services, assemblies);

        var brokerConfig = configuration.GetSection("MessageBroker").Get<MessageBrokerConfig>()
                            ?? new MessageBrokerConfig();

        ValidateBrokerConfig(brokerConfig);

        bool isKafka = brokerConfig.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase);

        // 1. DI Event Bus Registration
        if (isKafka)
        {
            services.AddScoped<IEventBus, KafkaEventBus>();
        }
        else
        {
            services.AddScoped<IEventBus, PublishEndpointEventBus>();
        }

        // 2. Transport Configuration
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            if (!isKafka)
            {
                busConfigurator.ConfigureTransactionalOutbox<TDbContext>(brokerConfig, configureOutbox);
                busConfigurator.ConfigureRabbitMqBus(brokerConfig, assemblies);
            }
            else
            {
                busConfigurator.ConfigureKafkaBus(brokerConfig, assemblies);
            }
        });

        return services;
    }

    private static void ValidateBrokerConfig(MessageBrokerConfig brokerConfig)
    {
        var validationResults = new List<DataValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                brokerConfig,
                new System.ComponentModel.DataAnnotations.ValidationContext(brokerConfig),
                validationResults,
                validateAllProperties: true))
        {
            var message = string.Join(Environment.NewLine, validationResults.Select(r => r.ErrorMessage));
            throw new OptionsValidationException(
                nameof(MessageBrokerConfig), typeof(MessageBrokerConfig), new[] { message });
        }
    }

    private static void RegisterIntegrationEventHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}