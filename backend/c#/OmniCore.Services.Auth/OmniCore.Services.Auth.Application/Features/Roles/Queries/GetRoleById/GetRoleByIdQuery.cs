// Features/Roles/Queries/GetRoleById/GetRoleByIdQuery.cs
namespace OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoleById;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Helpers;

public record GetRoleByIdQuery(Guid RoleId) : ICacheableQuery<RoleResponse>
{
    public string CacheKey => CacheKeyGeneratorHelper.For<GetRoleByIdQuery>(RoleId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(30);
}