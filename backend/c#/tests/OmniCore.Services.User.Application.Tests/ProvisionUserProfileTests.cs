using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;
using OmniCore.Services.User.Application.Features.Profiles.IntegrationEvents;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.User.Application.Tests;

public class ProvisionUserProfileTests
{
    [Fact]
    public async Task Creates_the_profile_with_the_account_id()
    {
        var accountId = Guid.NewGuid();
        var repo = TestSupport.Repository(null);
        repo.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await new ProvisionUserProfileCommandHandler(repo).Handle(
            new ProvisionUserProfileCommand(accountId, "alice_01", "alice@example.com", null, null), default);

        Assert.True(result.IsSuccess);
        repo.Received(1).Add(Arg.Is<UserProfile>(p => p.Id.Value == accountId && p.Username.Value == "alice_01"));
    }

    [Fact]
    public async Task A_second_delivery_of_the_same_event_is_a_success_and_adds_nothing()
    {
        var repo = TestSupport.Repository(null);
        repo.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new ProvisionUserProfileCommandHandler(repo).Handle(
            new ProvisionUserProfileCommand(Guid.NewGuid(), "alice_01", null, null, null), default);

        Assert.True(result.IsSuccess);
        repo.DidNotReceive().Add(Arg.Any<UserProfile>());
    }

    [Fact]
    public async Task An_invalid_username_fails_and_adds_nothing()
    {
        var repo = TestSupport.Repository(null);

        var result = await new ProvisionUserProfileCommandHandler(repo).Handle(
            new ProvisionUserProfileCommand(Guid.NewGuid(), "ab", null, null, null), default);

        Assert.True(result.IsFailure);
        repo.DidNotReceive().Add(Arg.Any<UserProfile>());
    }

    [Fact]
    public async Task The_event_handler_does_not_store_the_username_as_a_first_name()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProvisionUserProfileCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var id = Guid.NewGuid();

        await new AccountRegisteredIntegrationEventHandler(sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance)
            .HandleAsync(new AccountRegisteredIntegrationEvent(id, "alice_01", "alice@example.com", "alice_01", ""));

        await sender.Received(1).Send(
            Arg.Is<ProvisionUserProfileCommand>(c =>
                c.AccountId == id && c.Username == "alice_01" && c.Email == "alice@example.com" && c.FirstName == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_event_handler_keeps_a_real_first_name()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProvisionUserProfileCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await new AccountRegisteredIntegrationEventHandler(sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance)
            .HandleAsync(new AccountRegisteredIntegrationEvent(Guid.NewGuid(), "alice_01", "a@b.co", "Alice", "Lee"));

        await sender.Received(1).Send(
            Arg.Is<ProvisionUserProfileCommand>(c => c.FirstName == "Alice" && c.LastName == "Lee"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_event_handler_throws_on_failure_so_the_broker_retries()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProvisionUserProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Validation("Username.Empty", "x")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AccountRegisteredIntegrationEventHandler(sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance)
                .HandleAsync(new AccountRegisteredIntegrationEvent(Guid.NewGuid(), "", "a@b.co", "", "")));
    }
}
