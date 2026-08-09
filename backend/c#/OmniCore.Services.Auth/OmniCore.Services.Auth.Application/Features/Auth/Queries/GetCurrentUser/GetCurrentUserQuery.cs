namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetCurrentUser;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record GetCurrentUserQuery : IQuery<AccountResponse>;