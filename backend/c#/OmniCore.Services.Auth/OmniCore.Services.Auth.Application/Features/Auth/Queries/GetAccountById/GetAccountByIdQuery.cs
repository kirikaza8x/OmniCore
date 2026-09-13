// Features/Auth/Queries/GetAccountById/GetAccountByIdQuery.cs
namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Helpers;

public record GetAccountByIdQuery(Guid AccountId) : ICacheableQuery<AccountResponse>
{
    public string CacheKey => CacheKeyGeneratorHelper.For<GetAccountByIdQuery>(AccountId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}