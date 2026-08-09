namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoles;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    ICacheService cacheService) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleResponse>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
    private const string CacheKey = "auth:roles:all";

    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        GetRolesQuery request, 
        CancellationToken cancellationToken)
    {
        var roles = await cacheService.GetOrCreateAsync(
            CacheKey,
            async ct =>
            {
                var roleEntities = await roleRepository.ListAsync(cancellationToken: ct);
                return roleEntities.Select(r => r.ToResponse()).ToList() as IReadOnlyList<RoleResponse>;
            },
            absoluteExpiration: CacheTtl,
            cancellationToken: cancellationToken);

        return Result.Success(roles ?? []);
    }
}