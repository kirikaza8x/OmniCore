namespace OmniCore.Shared.Infrastructure.Services.Authentication;

using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.DDD;
using TenantIdValue = OmniCore.Shared.Domain.DDD.TenantId;

/// <summary>
/// Scoped tenant context (ADR 0001). Order: system scope, explicit scope (consumers and jobs), then the token's
/// <c>tenant_id</c> claim. Request headers and bodies are never read.
/// </summary>
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public const string ClaimType = "tenant_id";

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

            string? claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimType)?.Value;
            return TenantIdValue.TryParse(claim, out TenantIdValue parsed) ? parsed : null;
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
