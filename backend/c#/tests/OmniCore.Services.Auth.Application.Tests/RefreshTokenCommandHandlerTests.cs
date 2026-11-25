using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.RefreshToken;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Shared.Domain.Repositories;

namespace OmniCore.Services.Auth.Application.Tests;

public class RefreshTokenCommandHandlerTests
{
    private readonly Domain.Repositories.IAccountRepository _accounts = TestSupport.Repository();
    private readonly IJwtTokenService _jwt = TestSupport.Jwt();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RefreshTokenCommandHandler Handler(params string[] newTokens) =>
        new(_accounts, _jwt, TestSupport.RefreshTokens(newTokens), _uow, TestSupport.NoTenants());

    private void Finds(Account? account) =>
        _accounts.GetByRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(account));

    private static Account AccountWithToken(string token)
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.AddRefreshToken(token, TestSupport.Week).IsSuccess);
        return account;
    }

    [Fact]
    public async Task UnknownToken_IsRejected()
    {
        Finds(null);

        var result = await Handler("new-1").Handle(new RefreshTokenCommand("nope"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task InactiveAccount_IsRejected()
    {
        var account = AccountWithToken("old-1");
        account.Deactivate();
        Finds(account);

        var result = await Handler("new-1").Handle(new RefreshTokenCommand("old-1"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task Success_RotatesTheToken_AndReturnsNewTokens()
    {
        var account = AccountWithToken("old-1");
        Finds(account);

        var result = await Handler("new-1").Handle(new RefreshTokenCommand("old-1"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-1", result.Value.RefreshToken);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.False(account.RefreshTokens.Single(t => t.Matches("old-1")).IsActive);
        Assert.True(account.RefreshTokens.Single(t => t.Matches("new-1")).IsActive);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReusedToken_IsRejected_AndRevocationIsSavedExplicitly()
    {
        var account = AccountWithToken("old-1");
        Finds(account);
        var handler = Handler("new-1", "new-2");
        Assert.True((await handler.Handle(new RefreshTokenCommand("old-1"), default)).IsSuccess);

        var reuse = await handler.Handle(new RefreshTokenCommand("old-1"), default);

        Assert.True(reuse.IsFailure);
        Assert.Equal("Auth.TokenReuseDetected", reuse.Error.Code);
        Assert.All(account.RefreshTokens, t => Assert.False(t.IsActive));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
