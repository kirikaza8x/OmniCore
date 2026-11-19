using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Events;

namespace OmniCore.Services.Auth.Domain.Tests;

// Written without a .NET SDK: these have not been compiled or run yet (see docs/repo-check.md).
public class AccountRefreshTokenTests
{
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    private static Account NewAccount()
    {
        var result = Account.Create("alice_01", "alice@example.com", "CHANGE_ME_hash");
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public void Create_RaisesAccountCreatedEvent()
    {
        var account = NewAccount();

        Assert.Contains(account.DomainEvents, e => e is AccountCreatedDomainEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("this_username_is_definitely_longer_than_thirty")]
    public void Create_RejectsInvalidUsername(string username)
    {
        var result = Account.Create(username, "alice@example.com", "CHANGE_ME_hash");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddRefreshToken_AttachesAnActiveToken()
    {
        var account = NewAccount();

        var result = account.AddRefreshToken("token-1", Week);

        Assert.True(result.IsSuccess);
        Assert.Single(account.RefreshTokens);
        Assert.True(account.RefreshTokens.Single().IsActive);
    }

    [Fact]
    public void AddRefreshToken_RejectsEmptyToken()
    {
        var result = NewAccount().AddRefreshToken(" ", Week);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RotateRefreshToken_RevokesTheOldTokenAndIssuesANewOne()
    {
        var account = NewAccount();
        account.AddRefreshToken("old", Week);

        var result = account.RotateRefreshToken("old", "new", Week);

        Assert.True(result.IsSuccess);
        var old = account.RefreshTokens.Single(t => t.Token == "old");
        Assert.True(old.IsRevoked);
        Assert.Equal("new", old.ReplacedByToken);
        Assert.True(account.RefreshTokens.Single(t => t.Token == "new").IsActive);
    }

    [Fact]
    public void RotateRefreshToken_ReusingARevokedTokenRevokesEverySession()
    {
        var account = NewAccount();
        account.AddRefreshToken("old", Week);
        account.RotateRefreshToken("old", "new", Week);

        var result = account.RotateRefreshToken("old", "attacker", Week);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.TokenReuseDetected", result.Error.Code);
        Assert.All(account.RefreshTokens, t => Assert.True(t.IsRevoked));
        Assert.DoesNotContain(account.RefreshTokens, t => t.Token == "attacker");
    }

    [Fact]
    public void RotateRefreshToken_UnknownTokenIsRejected()
    {
        var result = NewAccount().RotateRefreshToken("nope", "new", Week);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public void RevokeRefreshToken_IsIdempotentForAnAlreadyRevokedToken()
    {
        var account = NewAccount();
        account.AddRefreshToken("t", Week);
        account.RevokeRefreshToken("t");

        var again = account.RevokeRefreshToken("t");

        Assert.True(again.IsSuccess);
    }

    [Fact]
    public void AddRefreshToken_WithSessionCap_RevokesTheOldestActiveToken()
    {
        var account = NewAccount();
        account.AddRefreshToken("a", Week);
        account.AddRefreshToken("b", Week);

        account.AddRefreshToken("c", Week, maxActiveSessions: 2);

        Assert.Equal(2, account.RefreshTokens.Count(t => t.IsActive));
        Assert.Contains(account.RefreshTokens, t => t.Token == "c" && t.IsActive);
    }
}
