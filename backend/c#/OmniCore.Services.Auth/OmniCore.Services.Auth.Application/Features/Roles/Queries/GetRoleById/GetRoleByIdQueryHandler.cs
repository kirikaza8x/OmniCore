namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoleById;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetRoleByIdQueryHandler(
    IRoleRepository roleRepository,
    ICacheService cacheService) : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"auth:role:id:{request.RoleId}";

        var roleDto = await cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var roleId = new RoleId(request.RoleId);
                var role = await roleRepository.GetByIdAsync(roleId, ct);
                return role?.ToResponse();
            },
            absoluteExpiration: CacheTtl,
            cancellationToken: cancellationToken);

        if (roleDto is null)
        {
            return Result.Failure<RoleResponse>(
                Error.NotFound("Role.NotFound", $"Role with ID '{request.RoleId}' was not found."));
        }

        return Result.Success(roleDto);
    }
}