using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Application.Templates;
using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.Repositories;

namespace OmniCore.Services.Notification.Application.Tests;

public sealed class SendNotificationCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 8, 0, 0, DateTimeKind.Utc);

    private readonly INotificationMessageRepository _messages = Substitute.For<INotificationMessageRepository>();
    private readonly INotificationChannel _email = Substitute.For<INotificationChannel>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationCommandHandlerTests()
    {
        _email.Channel.Returns(NotificationChannel.Email);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        _handler = new SendNotificationCommandHandler(
            _messages, [_email], new BuiltInTemplateRenderer(), _uow, clock, NullLogger<SendNotificationCommandHandler>.Instance);
    }

    private static SendNotificationCommand Command(string? key = "welcome:1", string recipient = "ann@example.com", string template = "welcome") =>
        new(NotificationChannel.Email, recipient, template,
            new Dictionary<string, string> { ["name"] = "Ann", ["appName"] = "OmniCore" }, key);

    [Fact]
    public async Task A_successful_send_is_recorded_as_sent()
    {
        _email.SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>()).Returns(DeliveryResult.Ok);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sent", result.Value.Status);
        Assert.Equal(1, result.Value.Attempts);
        _messages.Received(1).Add(Arg.Any<NotificationMessage>());
        await _email.Received(1).SendAsync(
            Arg.Is<OutgoingNotification>(n => n.Recipient == "ann@example.com" && n.IsHtml && n.Subject == "Welcome to OmniCore"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_temporary_failure_is_saved_and_returned_as_a_failure_so_the_caller_retries()
    {
        _email.SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>()).Returns(DeliveryResult.Transient("smtp down"));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.DeliveryFailed", result.Error.Code);
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>()); // first claims the dedup key, then records the failure (the pipeline saves only on success)
    }

    [Fact]
    public async Task A_permanent_failure_is_saved_as_failed_and_is_not_retried()
    {
        _email.SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>()).Returns(DeliveryResult.Permanent("550 no such user"));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Failed", result.Value.Status);
        Assert.Equal("550 no such user", result.Value.LastError);
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>()); // claim + failure log
    }

    [Fact]
    public async Task A_redelivered_request_for_a_sent_message_does_not_send_again()
    {
        var sent = NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", "welcome:1", Now).Value;
        sent.MarkSent("Welcome to OmniCore", Now);
        _messages.GetByDeduplicationKeyAsync("welcome:1", Arg.Any<CancellationToken>()).Returns(sent);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _email.DidNotReceive().SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>());
        _messages.DidNotReceive().Add(Arg.Any<NotificationMessage>());
    }

    [Fact]
    public async Task A_duplicate_event_does_not_send_while_the_first_attempt_is_claimed()
    {
        var claimed = NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", "welcome:1", Now).Value;
        _messages.GetByDeduplicationKeyAsync("welcome:1", Arg.Any<CancellationToken>()).Returns(claimed);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _email.DidNotReceive().SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>());
        _messages.DidNotReceive().Add(Arg.Any<NotificationMessage>());
    }

    [Fact]
    public async Task An_abandoned_claim_is_retried_after_the_lease_expires()
    {
        var stale = NotificationMessage.Create(
            NotificationChannel.Email, "ann@example.com", "welcome", "welcome:1",
            Now - NotificationMessage.ClaimLease - TimeSpan.FromSeconds(1)).Value;
        _messages.GetByDeduplicationKeyAsync("welcome:1", Arg.Any<CancellationToken>()).Returns(stale);
        _email.SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>()).Returns(DeliveryResult.Ok);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("Sent", result.Value.Status);
        await _email.Received(1).SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_redelivered_request_for_a_pending_message_continues_the_same_message()
    {
        var pending = NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", "welcome:1", Now).Value;
        pending.MarkAttemptFailed("down", permanent: false, Now);
        _messages.GetByDeduplicationKeyAsync("welcome:1", Arg.Any<CancellationToken>()).Returns(pending);
        _email.SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>()).Returns(DeliveryResult.Ok);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("Sent", result.Value.Status);
        Assert.Equal(2, result.Value.Attempts);
        _messages.DidNotReceive().Add(Arg.Any<NotificationMessage>());
    }

    [Fact]
    public async Task An_unknown_template_sends_nothing_and_stores_nothing()
    {
        var result = await _handler.Handle(Command(template: "nope"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.TemplateNotFound", result.Error.Code);
        await _email.DidNotReceive().SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>());
        _messages.DidNotReceive().Add(Arg.Any<NotificationMessage>());
    }

    [Fact]
    public async Task A_bad_recipient_sends_nothing()
    {
        var result = await _handler.Handle(Command(recipient: "not-an-email"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.RecipientInvalid", result.Error.Code);
        await _email.DidNotReceive().SendAsync(Arg.Any<OutgoingNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_channel_with_no_implementation_is_reported()
    {
        var command = Command() with { Channel = NotificationChannel.Webhook, Recipient = "ops" };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.ChannelNotAvailable", result.Error.Code);
    }
}
