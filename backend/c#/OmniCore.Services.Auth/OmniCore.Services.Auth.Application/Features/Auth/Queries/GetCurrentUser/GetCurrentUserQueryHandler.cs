// Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs
namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetCurrentUser;

using MediatR;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService,
    ISender sender) : IQueryHandler<GetCurrentUserQuery, AccountResponse>
{
    public async Task<Result<AccountResponse>> Handle(
        GetCurrentUserQuery request, 
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
        {
            return Result.Failure<AccountResponse>(
                Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        return await sender.Send(new GetAccountByIdQuery(userId.Value), cancellationToken);
    }
}