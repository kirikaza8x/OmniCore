namespace OmniCore.Shared.Infrastructure.Services.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.DDD;
using TenantIdValue = OmniCore.Shared.Domain.DDD.TenantId;

/// <summary>
/// Scoped tenant context (ADR 0001). Order: system scope, explicit scope (consumers and jobs), then the token's
/// <c>tenant_id</c> claim. Request headers and bodies are never read.
/// </summary>
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : ITenantContext
{
    public const string ClaimType = "tenant_id";

    /// <summary>Transition window (ADR 0001, step 3): a signed-in caller whose token has no tenant claim is treated as this tenant. Unset = no fallback.</summary>
    public const string AssumeDefaultTenantKey = "Tenancy:AssumeDefaultTenantId";

    private TenantId? _explicit;
    private bool _system;

    public bool IsSystem => _system;

    public TenantId? TenantId
    {
        get
        {
            if (_system)
            {
                return null;
            }

            if (_explicit is not null)
            {
                return _explicit;
            }

            var user = httpContextAccessor.HttpContext?.User;
            if (TenantIdValue.TryParse(user?.FindFirst(ClaimType)?.Value, out TenantIdValue parsed))
            {
                return parsed;
            }

            // Only for a signed-in caller: an anonymous call never gets a tenant.
            if (user?.Identity?.IsAuthenticated == true
                && TenantIdValue.TryParse(configuration[AssumeDefaultTenantKey], out TenantIdValue assumed))
            {
                return assumed;
            }

            return null;
        }
    }

    public IDisposable BeginTenantScope(TenantId tenantId)
    {
        TenantId? previous = _explicit;
        bool previousSystem = _system;
        _explicit = tenantId;
        _system = false;
        return new Scope(() => { _explicit = previous; _system = previousSystem; });
    }

    public IDisposable BeginSystemScope()
    {
        bool previous = _system;
        _system = true;
        return new Scope(() => _system = previous);
    }

    private sealed class Scope(Action restore) : IDisposable
    {
        private Action? _restore = restore;

        public void Dispose()
        {
            _restore?.Invoke();
            _restore = null;
        }
    }
}
