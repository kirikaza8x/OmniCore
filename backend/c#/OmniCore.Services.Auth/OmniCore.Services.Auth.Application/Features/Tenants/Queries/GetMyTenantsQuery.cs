namespace OmniCore.Services.Auth.Application.Features.Tenants.Queries;

using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>The tenants the signed-in account belongs to (active tenants only).</summary>
public record GetMyTenantsQuery : IQuery<IReadOnlyList<TenantDto>>;

public sealed class GetMyTenantsQueryHandler(
    ITenantRepository tenants,
    ICurrentUserService currentUser,
    ITenantContext tenantContext) : IQueryHandler<GetMyTenantsQuery, IReadOnlyList<TenantDto>>
{
    public async Task<Result<IReadOnlyList<TenantDto>>> Handle(GetMyTenantsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<TenantDto>>(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        var memberships = await tenants.GetMembershipsAsync(new AccountId(userId), cancellationToken);

        IReadOnlyList<TenantDto> dtos = memberships
            .Select(m => new TenantDto(m.TenantId.Value, m.Slug, m.Name, m.Role, tenantContext.TenantId?.Value == m.TenantId.Value))
            .ToList();

        return Result.Success(dtos);
    }
}
