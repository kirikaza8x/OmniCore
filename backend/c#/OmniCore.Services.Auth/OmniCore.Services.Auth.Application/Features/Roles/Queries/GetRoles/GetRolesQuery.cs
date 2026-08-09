using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoles;


public record GetRolesQuery : IQuery<IReadOnlyList<RoleResponse>>;

