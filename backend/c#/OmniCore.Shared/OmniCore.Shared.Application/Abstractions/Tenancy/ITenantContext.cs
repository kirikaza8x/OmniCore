namespace OmniCore.Shared.Application.Abstractions.Tenancy;

using OmniCore.Shared.Domain.DDD;

/// <summary>
/// The active tenant of the current operation (ADR 0001). For a request it comes from the token's
/// <c>tenant_id</c> claim and never from the body or a header. A consumer or job sets it explicitly from the message.
/// </summary>
public interface ITenantContext
{
    /// <summary>The active tenant, or null when there is none (anonymous call, token without the claim).</summary>
    TenantId? TenantId { get; }

    /// <summary>True inside <see cref="BeginSystemScope"/>: tenant filters and checks are off.</summary>
    bool IsSystem { get; }

    /// <summary>Runs the following work as the given tenant until the scope is disposed (consumers, jobs).</summary>
    IDisposable BeginTenantScope(TenantId tenantId);

    /// <summary>Explicit cross-tenant scope (migrations, admin jobs). Use sparingly: it is the only way past the filters.</summary>
    IDisposable BeginSystemScope();
}

/// <summary>Marks a command or query that needs an active tenant. Without one the pipeline answers 403.</summary>
public interface ITenantScopedRequest;
