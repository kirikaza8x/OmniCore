using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;

namespace OmniCore.Services.Auth.Application.Tests;

internal static class TestSupport
{
    public static readonly TimeSpan Week = TimeSpan.FromDays(7);
    public const string Password = "Passw0rd!";

    public static Account NewAccount(string username = "alice_01", string email = "alice@example.com")
    {
        var result = Account.Create(username, email, "CHANGE_ME_hash");
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    public static IJwtTokenService Jwt()
    {
        var jwt = Substitute.For<IJwtTokenService>();
        jwt.ExpiryMinutes.Returns(15);
        jwt.GenerateToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<IEnumerable<string>>())
            .Returns("access-token");
        return jwt;
    }

    public static IRefreshTokenService RefreshTokens(params string[] tokens)
    {
        var service = Substitute.For<IRefreshTokenService>();
        var queue = new Queue<string>(tokens);
        service.GenerateRefreshToken().Returns(_ => (queue.Dequeue(), Week));
        return service;
    }

    public static IAccountRepository Repository() => Substitute.For<IAccountRepository>();
}
