using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.Commands;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.ConfirmEmail;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestEmailVerification;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestPasswordReset;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.ResetPassword;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.EventBus;

namespace OmniCore.Services.Auth.Application.Tests;

/// <summary>Password reset and e-mail verification (ADR 0009).</summary>
public class SecurityTokenFlowTests
{
    private readonly IAccountRepository _accounts = TestSupport.Repository();
    private readonly IEventBus _bus = Substitute.For<IEventBus>();
    private readonly IPasswordHasherService _hasher = Substitute.For<IPasswordHasherService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public SecurityTokenFlowTests() => _hasher.HashPassword(Arg.Any<string>()).Returns("CHANGE_ME_new_hash");

    private static string Issue(Account account, SecurityTokenType type, TimeSpan? lifetime = null)
    {
        var code = SecurityToken.GenerateCode();
        Assert.True(account.IssueSecurityToken(type, SecurityToken.HashCode(code), lifetime ?? TimeSpan.FromHours(1)).IsSuccess);
        return code;
    }

    private void FindsByToken(Account? account, SecurityTokenType type) =>
        _accounts.GetBySecurityTokenAsync(Arg.Any<string>(), type, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(account));

    // ---- forgot password

    [Fact]
    public async Task Forgot_UnknownEmail_SucceedsAndSendsNothing()
    {
        _accounts.GetByEmailWithSecurityTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(null));

        var result = await new RequestPasswordResetCommandHandler(_accounts, _bus)
            .Handle(new RequestPasswordResetCommand("nobody@example.com"), default);

        Assert.True(result.IsSuccess);
        await _bus.DidNotReceiveWithAnyArgs().PublishAsync<PasswordResetRequestedIntegrationEvent>(default!, default);
    }

    [Fact]
    public async Task Forgot_KnownEmail_StoresOnlyTheHashAndPublishesTheRawToken()
    {
        var account = TestSupport.NewAccount();
        _accounts.GetByEmailWithSecurityTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));
        PasswordResetRequestedIntegrationEvent? sent = null;
        await _bus.PublishAsync(Arg.Do<PasswordResetRequestedIntegrationEvent>(e => sent = e), Arg.Any<CancellationToken>());

        var result = await new RequestPasswordResetCommandHandler(_accounts, _bus)
            .Handle(new RequestPasswordResetCommand("alice@example.com"), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sent);
        var stored = Assert.Single(account.SecurityTokens);
        Assert.Equal(SecurityTokenType.PasswordReset, stored.TokenType);
        Assert.Equal(SecurityToken.HashCode(sent!.Token), stored.CodeHash);
        Assert.NotEqual(sent.Token, stored.CodeHash);
        Assert.Equal(60, sent.ExpiresInMinutes);
    }

    [Fact]
    public async Task Forgot_Twice_OnlyTheNewestLinkWorks()
    {
        var account = TestSupport.NewAccount();
        var first = Issue(account, SecurityTokenType.PasswordReset);
        var second = Issue(account, SecurityTokenType.PasswordReset);

        Assert.True(account.RedeemSecurityToken(SecurityTokenType.PasswordReset, SecurityToken.HashCode(first)).IsFailure);
        Assert.True(account.RedeemSecurityToken(SecurityTokenType.PasswordReset, SecurityToken.HashCode(second)).IsSuccess);
    }

    // ---- reset password

    [Fact]
    public async Task Reset_WithValidToken_ChangesPasswordRevokesSessionsAndUsesTheToken()
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.AddRefreshToken("session-1", TestSupport.Week).IsSuccess);
        var code = Issue(account, SecurityTokenType.PasswordReset);
        FindsByToken(account, SecurityTokenType.PasswordReset);
        var handler = new ResetPasswordCommandHandler(_accounts, _hasher);

        var result = await handler.Handle(new ResetPasswordCommand(code, "N3w-Passw0rd!"), default);

        Assert.True(result.IsSuccess);
        _hasher.Received(1).HashPassword("N3w-Passw0rd!");
        Assert.All(account.RefreshTokens, t => Assert.True(t.IsRevoked));
        Assert.All(account.SecurityTokens, t => Assert.True(t.IsUsed));

        var again = await handler.Handle(new ResetPasswordCommand(code, "N3w-Passw0rd!"), default);
        Assert.True(again.IsFailure);
        Assert.Equal("Auth.InvalidToken", again.Error.Code);
    }

    [Fact]
    public async Task Reset_UnknownToken_IsRejected()
    {
        FindsByToken(null, SecurityTokenType.PasswordReset);

        var result = await new ResetPasswordCommandHandler(_accounts, _hasher)
            .Handle(new ResetPasswordCommand("nope", "N3w-Passw0rd!"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidToken", result.Error.Code);
        _hasher.DidNotReceive().HashPassword(Arg.Any<string>());
    }

    [Fact]
    public void Token_OfAnotherType_CannotBeRedeemed()
    {
        var account = TestSupport.NewAccount();
        var verifyCode = Issue(account, SecurityTokenType.EmailVerification);

        var result = account.RedeemSecurityToken(SecurityTokenType.PasswordReset, SecurityToken.HashCode(verifyCode));

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task Token_AfterItsLifetime_CannotBeRedeemed()
    {
        var account = TestSupport.NewAccount();
        var code = Issue(account, SecurityTokenType.PasswordReset, TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);

        var result = account.RedeemSecurityToken(SecurityTokenType.PasswordReset, SecurityToken.HashCode(code));

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("NoDigits!!")]
    public async Task ResetValidator_RejectsWeakPasswords(string password)
    {
        var validation = await new ResetPasswordCommandValidator().ValidateAsync(new ResetPasswordCommand("token", password));

        Assert.False(validation.IsValid);
    }

    // ---- e-mail verification

    [Fact]
    public async Task VerifyRequest_NotSignedIn_IsUnauthorized()
    {
        _user.UserId.Returns((Guid?)null);

        var result = await new RequestEmailVerificationCommandHandler(_user, _accounts, _bus)
            .Handle(new RequestEmailVerificationCommand(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task VerifyRequest_UnconfirmedEmail_IssuesATokenAndPublishes()
    {
        var account = TestSupport.NewAccount();
        _user.UserId.Returns(account.Id.Value);
        _accounts.GetByIdWithSecurityTokensAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));
        EmailVerificationRequestedIntegrationEvent? sent = null;
        await _bus.PublishAsync(Arg.Do<EmailVerificationRequestedIntegrationEvent>(e => sent = e), Arg.Any<CancellationToken>());

        var result = await new RequestEmailVerificationCommandHandler(_user, _accounts, _bus)
            .Handle(new RequestEmailVerificationCommand(), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sent);
        Assert.Equal("alice@example.com", sent!.Email);
        Assert.Equal(SecurityToken.HashCode(sent.Token), Assert.Single(account.SecurityTokens).CodeHash);
        Assert.Equal(1440, sent.ExpiresInMinutes);
    }

    [Fact]
    public async Task VerifyRequest_AlreadyConfirmed_DoesNothing()
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.ConfirmEmail().IsSuccess);
        _user.UserId.Returns(account.Id.Value);
        _accounts.GetByIdWithSecurityTokensAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));

        var result = await new RequestEmailVerificationCommandHandler(_user, _accounts, _bus)
            .Handle(new RequestEmailVerificationCommand(), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(account.SecurityTokens);
        await _bus.DidNotReceiveWithAnyArgs().PublishAsync<EmailVerificationRequestedIntegrationEvent>(default!, default);
    }

    [Fact]
    public async Task Confirm_WithValidToken_ConfirmsTheEmailOnce()
    {
        var account = TestSupport.NewAccount();
        var code = Issue(account, SecurityTokenType.EmailVerification, SecurityTokenLifetimes.EmailVerification);
        FindsByToken(account, SecurityTokenType.EmailVerification);
        var handler = new ConfirmEmailCommandHandler(_accounts);

        var result = await handler.Handle(new ConfirmEmailCommand(code), default);

        Assert.True(result.IsSuccess);
        Assert.True(account.IsEmailConfirmed);
        Assert.True((await handler.Handle(new ConfirmEmailCommand(code), default)).IsFailure);
    }

    [Fact]
    public async Task Confirm_UnknownToken_IsRejected()
    {
        FindsByToken(null, SecurityTokenType.EmailVerification);

        var result = await new ConfirmEmailCommandHandler(_accounts).Handle(new ConfirmEmailCommand("nope"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidToken", result.Error.Code);
    }
}
