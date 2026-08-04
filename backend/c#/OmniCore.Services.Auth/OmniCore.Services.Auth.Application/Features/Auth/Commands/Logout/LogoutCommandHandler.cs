namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Logout;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record LogoutCommand(
    string RefreshToken) : ICommand;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenService refreshTokenService) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(
        LogoutCommand request, 
        CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token is null)
        {
            // Idempotent success if token does not exist or was already removed
            return Result.Success();
        }

        refreshTokenService.RevokeToken(token);
        // UoW Behavior auto-commits on Result.Success

        return Result.Success();
    }
}