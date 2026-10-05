namespace OmniCore.Shared.Infrastructure.Configs.MessageBroker;

using System.ComponentModel.DataAnnotations;

public sealed class MessageBrokerConfig : ConfigBase, IValidatableObject
{
    public override string SectionName => "MessageBroker";

    [Required]
    public string Provider { get; set; } = "RabbitMQ"; // "RabbitMQ" or "Kafka"

    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string KafkaBootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Database provider for MassTransit's internal outbox: "Postgres", "SqlServer", "MySql", "Sqlite".
    /// </summary>
    public string OutboxDbProvider { get; set; } = "Postgres";

    /// <summary>
    /// Enables MassTransit's EF Core transactional outbox.
    /// Note: This only applies when Provider is "RabbitMQ". Setting this true for "Kafka"
    /// is rejected during startup validation because Kafka Rider producers operate outside the Bus Outbox.
    /// </summary>
    public bool EnableOutbox { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var provider = Provider?.Trim();
        bool isRabbit = string.Equals(provider, "RabbitMQ", StringComparison.OrdinalIgnoreCase);
        bool isKafka = string.Equals(provider, "Kafka", StringComparison.OrdinalIgnoreCase);

        if (!isRabbit && !isKafka)
        {
            yield return new ValidationResult(
                $"MessageBroker:Provider must be 'RabbitMQ' or 'Kafka'. Got '{Provider}'.",
                new[] { nameof(Provider) });
        }

        if (isRabbit && string.IsNullOrWhiteSpace(Host))
        {
            yield return new ValidationResult(
                "MessageBroker:Host is required when Provider is 'RabbitMQ'.",
                new[] { nameof(Host) });
        }

        if (isKafka && string.IsNullOrWhiteSpace(KafkaBootstrapServers))
        {
            yield return new ValidationResult(
                "MessageBroker:KafkaBootstrapServers is required when Provider is 'Kafka'.",
                new[] { nameof(KafkaBootstrapServers) });
        }

        if (EnableOutbox && isKafka)
        {
            yield return new ValidationResult(
                "MessageBroker:EnableOutbox=true is not supported with Provider='Kafka'. " +
                "The MassTransit EF Core outbox only covers RabbitMQ bus traffic, not Kafka Rider producers. " +
                "Set Provider='RabbitMQ', or set EnableOutbox=false.",
                new[] { nameof(EnableOutbox), nameof(Provider) });
        }
    }
}