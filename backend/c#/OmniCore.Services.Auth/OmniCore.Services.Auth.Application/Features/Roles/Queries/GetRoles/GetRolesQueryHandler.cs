// Features/Roles/Queries/GetRoles/GetRolesQueryHandler.cs
namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoles;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleResponse>>
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        GetRolesQuery request, 
        CancellationToken cancellationToken)
    {
        var roleEntities = await roleRepository.ListAsync(cancellationToken: cancellationToken);
        var responses = roleEntities.Select(r => r.ToResponse()).ToList();

        return Result.Success<IReadOnlyList<RoleResponse>>(responses);
    }
}