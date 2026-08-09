namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.UpdateRole;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    ICacheService cacheService) : ICommandHandler<UpdateRoleCommand, RoleResponse>
{
    private const string RolesListCacheKey = "auth:roles:all";

    public async Task<Result<RoleResponse>> Handle(
        UpdateRoleCommand request, 
        CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.RoleId);
        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<RoleResponse>(
                Error.NotFound("Role.NotFound", $"Role with ID '{request.RoleId}' was not found."));
        }

        if (!role.Name.Equals(request.NewName, StringComparison.OrdinalIgnoreCase))
        {
            var isUnique = await roleRepository.IsNameUniqueAsync(request.NewName, cancellationToken);
            if (!isUnique)
            {
                return Result.Failure<RoleResponse>(
                    Error.Conflict("Role.NameAlreadyExists", $"Role with name '{request.NewName}' already exists."));
            }
        }

        var updateResult = role.UpdateName(request.NewName);
        if (updateResult.IsFailure)
        {
            return Result.Failure<RoleResponse>(updateResult.Error);
        }

        // Evict stale cached state
        var singleRoleCacheKey = $"auth:role:id:{request.RoleId}";
        await cacheService.RemoveAsync(singleRoleCacheKey, cancellationToken);
        await cacheService.RemoveAsync(RolesListCacheKey, cancellationToken);

        return Result.Success(role.ToResponse());
    }
}