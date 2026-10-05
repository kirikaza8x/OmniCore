// Features/UserProfiles/Queries/GetUserProfileById/GetUserProfileByIdQuery.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Queries.GetUserProfileById;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Helpers;

public record GetUserProfileByIdQuery(Guid UserId) : ICacheableQuery<UserProfileResponse>
{
    public string CacheKey => CacheKeyGeneratorHelper.For<GetUserProfileByIdQuery>(UserId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(30);
}