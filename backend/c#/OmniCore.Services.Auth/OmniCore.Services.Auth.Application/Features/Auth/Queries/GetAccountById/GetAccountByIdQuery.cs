namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountResponse>;