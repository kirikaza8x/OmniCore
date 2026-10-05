// Features/Roles/Queries/GetRoleById/GetRoleByIdQueryHandler.cs
namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoleById;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetRoleByIdQueryHandler(
    IRoleRepository roleRepository) : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.RoleId);
        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<RoleResponse>(
                Error.NotFound("Role.NotFound", $"Role with ID '{request.RoleId}' was not found."));
        }

        return Result.Success(role.ToResponse());
    }
}