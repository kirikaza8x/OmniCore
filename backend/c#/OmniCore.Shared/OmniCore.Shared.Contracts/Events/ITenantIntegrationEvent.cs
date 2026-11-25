namespace OmniCore.Shared.Contracts.Events;

/// <summary>
/// Implement on an integration event that belongs to one tenant (ADR 0001). The consumer sets the tenant context from
/// <see cref="TenantId"/> before handling. Combine with <see cref="IPartitionedIntegrationEvent"/> and build the
/// partition key with <see cref="TenantPartitionKey.For"/> so one tenant's events for one aggregate stay ordered.
/// </summary>
public interface ITenantIntegrationEvent
{
    Guid TenantId { get; }
}

public static class TenantPartitionKey
{
    public static string For(Guid tenantId, Guid aggregateId) => $"{tenantId:N}:{aggregateId:N}";
}
