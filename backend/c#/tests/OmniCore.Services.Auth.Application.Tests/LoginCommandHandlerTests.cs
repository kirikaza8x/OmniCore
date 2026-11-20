using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;
using OmniCore.Services.Auth.Domain.Entities;

namespace OmniCore.Services.Auth.Application.Tests;

public class LoginCommandHandlerTests
{
    private readonly Domain.Repositories.IAccountRepository _accounts = TestSupport.Repository();
    private readonly IPasswordHasherService _hasher = Substitute.For<IPasswordHasherService>();
    private readonly IJwtTokenService _jwt = TestSupport.Jwt();
    private readonly IRefreshTokenService _refresh = TestSupport.RefreshTokens("refresh-1");

    private LoginCommandHandler Handler() => new(_accounts, _hasher, _jwt, _refresh);

    private void Finds(Account? account) =>
        _accounts.GetByIdentifierAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(account));

    [Fact]
    public async Task UnknownIdentifier_ReturnsNotFound()
    {
        Finds(null);

        var result = await Handler().Handle(new LoginCommand("nobody", TestSupport.Password), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.AccountNotFound", result.Error.Code);
    }

    [Fact]
    public async Task InactiveAccount_IsRejected_BeforeThePasswordIsChecked()
    {
        var account = TestSupport.NewAccount();
        account.Deactivate();
        Finds(account);

        var result = await Handler().Handle(new LoginCommand("alice_01", TestSupport.Password), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.AccountInactive", result.Error.Code);
        _hasher.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorized_AndIssuesNoToken()
    {
        var account = TestSupport.NewAccount();
        Finds(account);
        _hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await Handler().Handle(new LoginCommand("alice_01", "wrong"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidPassword", result.Error.Code);
        Assert.Empty(account.RefreshTokens);
        _jwt.DidNotReceiveWithAnyArgs().GenerateToken(default, default, default, default!);
    }

    [Fact]
    public async Task Success_AttachesRefreshToken_AndReturnsTokens()
    {
        var account = TestSupport.NewAccount();
        Finds(account);
        _hasher.VerifyPassword(TestSupport.Password, Arg.Any<string>()).Returns(true);

        var result = await Handler().Handle(new LoginCommand("alice_01", TestSupport.Password), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
        Assert.Single(account.RefreshTokens);
        Assert.Equal("refresh-1", account.RefreshTokens.Single().Token);
    }

    [Fact]
    public async Task Identifier_IsTrimmedBeforeTheLookup()
    {
        Finds(null);

        await Handler().Handle(new LoginCommand("  alice_01  ", TestSupport.Password), default);

        await _accounts.Received(1).GetByIdentifierAsync("alice_01", Arg.Any<CancellationToken>());
    }
}
