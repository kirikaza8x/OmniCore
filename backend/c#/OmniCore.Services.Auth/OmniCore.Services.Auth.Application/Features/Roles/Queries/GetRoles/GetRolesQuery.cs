// Features/Roles/Queries/GetRoles/GetRolesQuery.cs
namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoles;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Helpers;

public record GetRolesQuery : ICacheableQuery<IReadOnlyList<RoleResponse>>
{
    public string CacheKey => CacheKeyGeneratorHelper.For<GetRolesQuery>();
    public TimeSpan? Expiration => TimeSpan.FromMinutes(30);
}