namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.CreateRole;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    ICacheService cacheService) : ICommandHandler<CreateRoleCommand, RoleResponse>
{
    private const string RolesCacheKey = "auth:roles:all";

    public async Task<Result<RoleResponse>> Handle(
        CreateRoleCommand request, 
        CancellationToken cancellationToken)
    {
        var isUnique = await roleRepository.IsNameUniqueAsync(request.Name, cancellationToken);
        if (!isUnique)
        {
            return Result.Failure<RoleResponse>(
                Error.Conflict("Role.NameAlreadyExists", $"Role with name '{request.Name}' already exists."));
        }

        var roleResult = Role.Create(request.Name);
        if (roleResult.IsFailure)
        {
            return Result.Failure<RoleResponse>(roleResult.Error);
        }

        var role = roleResult.Value;
        roleRepository.Add(role);

        // Evict list cache so subsequent GetRoles queries fetch the fresh entity
        await cacheService.RemoveAsync(RolesCacheKey, cancellationToken);

        return Result.Success(role.ToResponse());
    }
}