namespace OmniCore.Services.Auth.Application.Features.Roles.Mappings;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Domain.Entities;

public static class RoleMappingExtensions
{
    public static RoleResponse ToResponse(this Role role) => new(
        role.Id.Value,
        role.Name,
        role.Description
    );

    public static List<RoleResponse> ToResponseList(this IEnumerable<Role> roles) =>
        roles.Select(r => r.ToResponse()).ToList();
}