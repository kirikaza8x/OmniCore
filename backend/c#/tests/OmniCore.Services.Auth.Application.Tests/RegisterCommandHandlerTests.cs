using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.Register;
using OmniCore.Services.Auth.Domain.Entities;

namespace OmniCore.Services.Auth.Application.Tests;

// Written and checked by CI only (no .NET SDK in the authoring sandbox).
public class RegisterCommandHandlerTests
{
    private readonly Domain.Repositories.IAccountRepository _accounts = TestSupport.Repository();
    private readonly IPasswordHasherService _hasher = Substitute.For<IPasswordHasherService>();
    private readonly IJwtTokenService _jwt = TestSupport.Jwt();
    private readonly IRefreshTokenService _refresh = TestSupport.RefreshTokens("refresh-1");

    private RegisterCommandHandler Handler() => new(_accounts, _hasher, _jwt, _refresh);

    private void Uniqueness(bool emailTaken, bool usernameTaken) =>
        _accounts.CheckUniquenessAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((emailTaken, usernameTaken));

    [Fact]
    public async Task EmailTaken_ReturnsConflict_AndDoesNotHashOrSave()
    {
        Uniqueness(emailTaken: true, usernameTaken: false);

        var result = await Handler().Handle(new RegisterCommand("alice_01", "alice@example.com", TestSupport.Password), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.EmailAlreadyInUse", result.Error.Code);
        _hasher.DidNotReceive().HashPassword(Arg.Any<string>());
        _accounts.DidNotReceive().Add(Arg.Any<Account>());
    }

    [Fact]
    public async Task UsernameTaken_ReturnsConflict()
    {
        Uniqueness(emailTaken: false, usernameTaken: true);

        var result = await Handler().Handle(new RegisterCommand("alice_01", "alice@example.com", TestSupport.Password), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.UsernameAlreadyInUse", result.Error.Code);
        _accounts.DidNotReceive().Add(Arg.Any<Account>());
    }

    [Fact]
    public async Task InvalidUsername_Fails_AndDoesNotSave()
    {
        Uniqueness(false, false);
        _hasher.HashPassword(Arg.Any<string>()).Returns("hashed");

        var result = await Handler().Handle(new RegisterCommand("ab", "alice@example.com", TestSupport.Password), default);

        Assert.True(result.IsFailure);
        _accounts.DidNotReceive().Add(Arg.Any<Account>());
    }

    [Fact]
    public async Task Success_HashesPassword_AddsAccount_AndIssuesTokensWithUserRole()
    {
        Uniqueness(false, false);
        _hasher.HashPassword(TestSupport.Password).Returns("hashed");

        var result = await Handler().Handle(new RegisterCommand("alice_01", "alice@example.com", TestSupport.Password), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
        Assert.Equal(15, result.Value.ExpiresInMinutes);
        _hasher.Received(1).HashPassword(TestSupport.Password);
        _accounts.Received(1).Add(Arg.Is<Account>(a =>
            a.Username.Value == "alice_01" && a.RefreshTokens.Count == 1 && a.PasswordHash!.Value == "hashed"));
        _jwt.Received(1).GenerateToken(
            Arg.Any<Guid>(), "alice@example.com", "alice_01",
            Arg.Is<IEnumerable<string>>(roles => roles.SequenceEqual(new[] { "User" })));
    }
}
