namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetCurrentUser;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService,
    IAccountRepository accountRepository,
    ICacheService cacheService) : IQueryHandler<GetCurrentUserQuery, AccountResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<AccountResponse>> Handle(
        GetCurrentUserQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Resolve User ID from JWT context
        var userId = currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
        {
            return Result.Failure<AccountResponse>(
                Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        // 2. Fetch via Cache
        var cacheKey = $"auth:account:id:{userId}";

        var accountDto = await cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => (await accountRepository.GetByIdAsync(new AccountId(userId.Value), ct))?.ToDto(),
            absoluteExpiration: CacheTtl,
            cancellationToken: cancellationToken);

        if (accountDto is null || !accountDto.IsActive)
        {
            return Result.Failure<AccountResponse>(
                Error.NotFound("Auth.AccountNotFound", "Account was not found or is inactive."));
        }

        // 3. Return safe response
        return Result.Success(accountDto.ToResponse());
    }
}