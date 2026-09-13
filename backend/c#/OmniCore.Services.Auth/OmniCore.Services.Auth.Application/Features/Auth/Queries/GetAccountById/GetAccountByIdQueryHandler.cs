// Features/Auth/Queries/GetAccountById/GetAccountByIdQueryHandler.cs
namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.Specifications;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetAccountByIdQueryHandler(
    IAccountRepository accountRepository) : IQueryHandler<GetAccountByIdQuery, AccountResponse>
{
    public async Task<Result<AccountResponse>> Handle(
        GetAccountByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var spec = new AccountWithRolesSpecification(new AccountId(request.AccountId));
        var account = await accountRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null)
        {
            return Result.Failure<AccountResponse>(
                Error.NotFound("Auth.AccountNotFound", $"Account with ID '{request.AccountId}' was not found."));
        }

        return Result.Success(account.ToDto().ToResponse());
    }
}