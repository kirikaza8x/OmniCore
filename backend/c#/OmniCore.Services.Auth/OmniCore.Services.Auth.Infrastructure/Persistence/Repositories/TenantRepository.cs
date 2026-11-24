namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Domain.DDD;

public class TenantRepository(AuthDbContext dbContext) : ITenantRepository
{
    public async Task<IReadOnlyList<TenantMembershipInfo>> GetMembershipsAsync(
        AccountId accountId, CancellationToken cancellationToken = default)
    {
        var rows = await (
                from m in dbContext.TenantMemberships.AsNoTracking()
                join t in dbContext.Tenants.AsNoTracking() on m.TenantId equals t.Id
                where m.AccountId == accountId && t.IsActive
                orderby m.JoinedAtUtc, t.Slug
                select new { m.TenantId, t.Slug, t.Name, m.Role, m.JoinedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new TenantMembershipInfo(r.TenantId, r.Slug, r.Name, r.Role, r.JoinedAtUtc))
            .ToList();
    }

    public Task<TenantMembership?> GetMembershipAsync(
        TenantId tenantId, AccountId accountId, CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.FirstOrDefaultAsync(
            m => m.TenantId == tenantId && m.AccountId == accountId, cancellationToken);

    public Task<Tenant?> GetByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);

    public void Add(Tenant tenant) => dbContext.Tenants.Add(tenant);

    public void Add(TenantMembership membership) => dbContext.TenantMemberships.Add(membership);
}
