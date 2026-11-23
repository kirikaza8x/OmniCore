using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;
using OmniCore.Services.Notification.Contracts.Events;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.Notification.Application.Tests;

public sealed class IntegrationEventHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private static Result<NotificationResponse> Ok() =>
        new NotificationResponse(Guid.NewGuid(), "Email", "a@example.com", "welcome", "s", "Sent", 1, null, DateTime.UtcNow, null, null);

    [Fact]
    public async Task A_new_account_gets_one_welcome_mail_keyed_by_the_account()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>()).Returns(Ok());
        var handler = new AccountRegisteredIntegrationEventHandler(_sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance);
        var accountId = Guid.NewGuid();

        await handler.HandleAsync(new AccountRegisteredIntegrationEvent(accountId, "ann", "ann@example.com", "Ann", "Lee"));

        await _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.Channel == NotificationChannel.Email && c.Recipient == "ann@example.com" && c.TemplateKey == "welcome"
                && c.DeduplicationKey == $"welcome:{accountId}" && c.Model["name"] == "Ann"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_username_is_used_when_there_is_no_first_name()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>()).Returns(Ok());
        var handler = new AccountRegisteredIntegrationEventHandler(_sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance);

        await handler.HandleAsync(new AccountRegisteredIntegrationEvent(Guid.NewGuid(), "ann", "ann@example.com", "", ""));

        await _sender.Received(1).Send(Arg.Is<SendNotificationCommand>(c => c.Model["name"] == "ann"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_temporary_failure_throws_so_the_broker_retries()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<NotificationResponse>(NotificationErrors.DeliveryFailed));
        var handler = new AccountRegisteredIntegrationEventHandler(_sender, NullLogger<AccountRegisteredIntegrationEventHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new AccountRegisteredIntegrationEvent(Guid.NewGuid(), "ann", "ann@example.com", "Ann", "Lee")));
    }

    [Fact]
    public async Task A_request_that_can_never_succeed_is_dropped_not_retried()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<NotificationResponse>(NotificationErrors.TemplateNotFound));
        var handler = new NotificationRequestedIntegrationEventHandler(_sender, NullLogger<NotificationRequestedIntegrationEventHandler>.Instance);

        await handler.HandleAsync(new NotificationRequestedIntegrationEvent("Email", "a@example.com", "nope", new Dictionary<string, string>()));
        // no exception: the message is acknowledged
    }

    [Fact]
    public async Task An_unknown_channel_is_dropped_without_calling_the_handler()
    {
        var handler = new NotificationRequestedIntegrationEventHandler(_sender, NullLogger<NotificationRequestedIntegrationEventHandler>.Instance);

        await handler.HandleAsync(new NotificationRequestedIntegrationEvent("Carrier-pigeon", "x", "welcome", new Dictionary<string, string>()));

        await _sender.DidNotReceive().Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Without_a_deduplication_key_the_event_id_is_used()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>()).Returns(Ok());
        var handler = new NotificationRequestedIntegrationEventHandler(_sender, NullLogger<NotificationRequestedIntegrationEventHandler>.Instance);
        var evt = new NotificationRequestedIntegrationEvent("email", "a@example.com", "welcome", new Dictionary<string, string>());

        await handler.HandleAsync(evt);

        await _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c => c.Channel == NotificationChannel.Email && c.DeduplicationKey == $"event:{evt.Id}"),
            Arg.Any<CancellationToken>());
    }
}
