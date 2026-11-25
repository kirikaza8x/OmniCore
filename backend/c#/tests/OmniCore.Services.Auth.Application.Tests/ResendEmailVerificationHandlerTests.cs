using OmniCore.Services.Auth.Application.Features.Auth.Commands.ResendEmailVerification;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.EventBus;

namespace OmniCore.Services.Auth.Application.Tests;

public class ResendEmailVerificationHandlerTests
{
    private readonly IAccountRepository _accounts = TestSupport.Repository();
    private readonly IEventBus _bus = Substitute.For<IEventBus>();

    private ResendEmailVerificationCommandHandler Handler() => new(_accounts, _bus);

    private void Finds(Account? account) =>
        _accounts.GetByEmailWithSecurityTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(account));

    [Fact]
    public async Task UnknownEmail_SucceedsAndSendsNothing()
    {
        Finds(null);

        var result = await Handler().Handle(new ResendEmailVerificationCommand("nobody@example.com"), default);

        Assert.True(result.IsSuccess);
        await _bus.DidNotReceive().PublishAsync(Arg.Any<EmailVerificationRequestedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlreadyVerified_SucceedsAndSendsNothing()
    {
        var account = TestSupport.NewAccount();
        Assert.True(account.ConfirmEmail().IsSuccess);
        Finds(account);

        var result = await Handler().Handle(new ResendEmailVerificationCommand("alice@example.com"), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(account.SecurityTokens);
        await _bus.DidNotReceive().PublishAsync(Arg.Any<EmailVerificationRequestedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unverified_GetsALink_AndTheOlderOneStopsWorking()
    {
        var account = TestSupport.NewAccount();
        var oldCode = SecurityToken.GenerateCode();
        Assert.True(account.IssueSecurityToken(SecurityTokenType.EmailVerification, SecurityToken.HashCode(oldCode), TimeSpan.FromHours(1)).IsSuccess);
        Finds(account);
        EmailVerificationRequestedIntegrationEvent? sent = null;
        await _bus.PublishAsync(Arg.Do<EmailVerificationRequestedIntegrationEvent>(e => sent = e), Arg.Any<CancellationToken>());

        var result = await Handler().Handle(new ResendEmailVerificationCommand("alice@example.com"), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sent);
        Assert.Equal(2, account.SecurityTokens.Count);
        Assert.True(account.RedeemSecurityToken(SecurityTokenType.EmailVerification, SecurityToken.HashCode(oldCode)).IsFailure);
        Assert.True(account.RedeemSecurityToken(SecurityTokenType.EmailVerification, SecurityToken.HashCode(sent!.Token)).IsSuccess);
    }
}
