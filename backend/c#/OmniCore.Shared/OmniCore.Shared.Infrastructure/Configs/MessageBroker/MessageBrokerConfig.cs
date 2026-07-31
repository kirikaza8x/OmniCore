namespace OmniCore.Shared.Infrastructure.Configs.MessageBroker;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration options for distributed message brokers.
/// </summary>
public sealed class MessageBrokerConfig : ConfigBase
{
    public override string SectionName => "MessageBroker";

    /// <summary>
    /// Broker transport type: "RabbitMQ", "Kafka", or "Both".
    /// </summary>
    [Required]
    public string Provider { get; set; } = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string KafkaBootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets whether to enable EF Core Transactional Outbox.
    /// </summary>
    public bool EnableOutbox { get; set; } = true;
}