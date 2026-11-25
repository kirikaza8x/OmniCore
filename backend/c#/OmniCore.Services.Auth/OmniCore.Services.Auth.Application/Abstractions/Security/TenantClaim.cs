namespace OmniCore.Services.Auth.Application.Abstractions.Security;

/// <summary>The active tenant written into an access token (ADR 0001): claims <c>tenant_id</c> and <c>tenant_role</c>.</summary>
public sealed record TenantClaim(Guid TenantId, string Role);
