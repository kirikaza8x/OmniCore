using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;
using OmniCore.Services.Notification.Application.Templates;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.Notification.Application.Tests;

/// <summary>Password reset and e-mail verification mails (ADR 0009).</summary>
public sealed class AccountSecurityMailTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly INotificationLinks _links = Substitute.For<INotificationLinks>();

    public AccountSecurityMailTests()
    {
        _links.ResetPassword(Arg.Any<string>()).Returns(c => $"https://app.example.com/reset-password?token={c.Arg<string>()}");
        _links.VerifyEmail(Arg.Any<string>()).Returns(c => $"https://app.example.com/verify-email?token={c.Arg<string>()}");
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResponse(Guid.NewGuid(), "Email", "a@example.com", "x", "s", "Sent", 1, null, DateTime.UtcNow, null, null));
    }

    private PasswordResetRequestedIntegrationEventHandler Reset() =>
        new(_sender, _links, NullLogger<PasswordResetRequestedIntegrationEventHandler>.Instance);

    private EmailVerificationRequestedIntegrationEventHandler Verify() =>
        new(_sender, _links, NullLogger<EmailVerificationRequestedIntegrationEventHandler>.Instance);

    [Fact]
    public async Task A_reset_request_sends_the_reset_mail_with_the_link_and_a_key_per_event()
    {
        var ev = new PasswordResetRequestedIntegrationEvent(Guid.NewGuid(), "ann", "ann@example.com", "tok-1", 60);

        await Reset().HandleAsync(ev);

        await _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.Channel == NotificationChannel.Email && c.Recipient == "ann@example.com" && c.TemplateKey == "password-reset"
                && c.DeduplicationKey == $"password-reset:{ev.Id}"
                && c.Model["link"] == "https://app.example.com/reset-password?token=tok-1"
                && c.Model["expiresInMinutes"] == "60" && c.Model["name"] == "ann"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_verification_request_sends_the_verify_mail()
    {
        var ev = new EmailVerificationRequestedIntegrationEvent(Guid.NewGuid(), "ann", "ann@example.com", "tok-2", 1440);

        await Verify().HandleAsync(ev);

        await _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.TemplateKey == "verify-email" && c.DeduplicationKey == $"verify-email:{ev.Id}"
                && c.Model["link"].EndsWith("token=tok-2") && c.Model["expiresInMinutes"] == "1440"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_temporary_failure_throws_so_the_broker_retries()
    {
        _sender.Send(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<NotificationResponse>(NotificationErrors.DeliveryFailed));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Reset().HandleAsync(new PasswordResetRequestedIntegrationEvent(Guid.NewGuid(), "ann", "ann@example.com", "t", 60)));
    }

    [Fact]
    public void The_templates_render_the_link_and_encode_it_in_html()
    {
        var renderer = new BuiltInTemplateRenderer();
        var model = new Dictionary<string, string>
        {
            ["name"] = "Ann", ["appName"] = "OmniCore", ["expiresInMinutes"] = "60",
            ["link"] = "https://app.example.com/reset-password?token=a-b_c&x=1",
        };

        var html = renderer.Render("password-reset", NotificationChannel.Email, model);
        var text = renderer.Render("password-reset", NotificationChannel.Webhook, model);
        var verify = renderer.Render("verify-email", NotificationChannel.Email, model);

        Assert.True(html.IsSuccess && text.IsSuccess && verify.IsSuccess);
        Assert.Contains("href=\"https://app.example.com/reset-password?token=a-b_c&amp;x=1\"", html.Value.Body);
        Assert.Contains("https://app.example.com/reset-password?token=a-b_c&x=1", text.Value.Body);
        Assert.Equal("Reset your OmniCore password", html.Value.Subject);
        Assert.Equal("Confirm your OmniCore e-mail", verify.Value.Subject);
    }
}
