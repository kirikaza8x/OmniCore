namespace OmniCore.Shared.Infrastructure.Configs.MessageBroker;

using System.ComponentModel.DataAnnotations;

public sealed class MessageBrokerConfig : ConfigBase
{
    public override string SectionName => "MessageBroker";

    [Required]
    public string Provider { get; set; } = "RabbitMQ"; // "RabbitMQ", "Kafka", "Both"

    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string KafkaBootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Database provider for MassTransit's internal outbox: "Postgres", "SqlServer", "MySql", "InMemory", or "CustomQuartz".
    /// </summary>
    public string OutboxDbProvider { get; set; } = "Postgres";

    public bool EnableOutbox { get; set; } = true;
}