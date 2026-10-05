namespace OmniCore.Shared.Contracts.Events;

/// <summary>
/// Implement on an IIntegrationEvent to control its Kafka partition key
/// (e.g. aggregate/entity id) so related events keep ordering on the same partition.
/// If not implemented, KafkaEventBus falls back to the event's Id.
/// </summary>
public interface IPartitionedIntegrationEvent
{
    string PartitionKey { get; }
}