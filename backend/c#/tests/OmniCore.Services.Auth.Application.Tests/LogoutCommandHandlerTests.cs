using OmniCore.Services.Auth.Application.Features.Auth.Commands.Logout;
using OmniCore.Services.Auth.Domain.Entities;

namespace OmniCore.Services.Auth.Application.Tests;

public class LogoutCommandHandlerTests
{
    private readonly Domain.Repositories.IAccountRepository _accounts = TestSupport.Repository();

    private LogoutCommandHandler Handler() => new(_accounts);

    [Fact]
    public async Task UnknownToken_IsAnIdempotentSuccess()
    {
        _accounts.GetByRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(null));

        var result = await Handler().Handle(new LogoutCommand("nope"), default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task KnownToken_IsRevoked()
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.AddRefreshToken("old-1", TestSupport.Week).IsSuccess);
        _accounts.GetByRefreshTokenAsync("old-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));

        var result = await Handler().Handle(new LogoutCommand("old-1"), default);

        Assert.True(result.IsSuccess);
        Assert.All(account.RefreshTokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public async Task LoggingOutTwice_StaysSuccessful()
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.AddRefreshToken("old-1", TestSupport.Week).IsSuccess);
        _accounts.GetByRefreshTokenAsync("old-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));

        await Handler().Handle(new LogoutCommand("old-1"), default);
        var second = await Handler().Handle(new LogoutCommand("old-1"), default);

        Assert.True(second.IsSuccess);
    }
}
