namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.Specifications;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetAccountByIdQueryHandler(
    IAccountRepository accountRepository,
    ICacheService cacheService) : IQueryHandler<GetAccountByIdQuery, AccountResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<AccountResponse>> Handle(
        GetAccountByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"auth:account:id:{request.AccountId}";

        var accountDto = await cacheService.GetOrCreateAsync(
    cacheKey,
    async ct =>
    {
        var spec = new AccountWithRolesSpecification(new AccountId(request.AccountId));
        var account = await accountRepository.FirstOrDefaultAsync(spec, ct);
        return account?.ToDto();
    },
    absoluteExpiration: CacheTtl,
    cancellationToken: cancellationToken);

        if (accountDto is null)
        {
            return Result.Failure<AccountResponse>(
                Error.NotFound("Auth.AccountNotFound", $"Account with ID '{request.AccountId}' was not found."));
        }

        return Result.Success(accountDto.ToResponse());
    }
}