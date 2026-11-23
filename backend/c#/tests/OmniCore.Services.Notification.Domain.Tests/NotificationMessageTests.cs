using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.Enums;

namespace OmniCore.Services.Notification.Domain.Tests;

public sealed class NotificationMessageTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 8, 0, 0, DateTimeKind.Utc);

    private static NotificationMessage NewEmail(string? key = "welcome:1") =>
        NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", key, Now).Value;

    [Fact]
    public void Create_starts_pending_with_no_attempts()
    {
        var m = NewEmail();
        Assert.Equal(NotificationStatus.Pending, m.Status);
        Assert.Equal(0, m.Attempts);
        Assert.Equal(Now, m.CreatedAt);
        Assert.Null(m.SentAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("Ann Lee <ann@example.com>")]        // display names are not addresses
    [InlineData("ann@example.com\r\nBcc: x@evil.test")] // header injection
    public void Create_rejects_bad_email_recipients(string recipient)
    {
        var result = NotificationMessage.Create(NotificationChannel.Email, recipient, "welcome", null, Now);
        Assert.True(result.IsFailure);
        Assert.Equal("Notification.RecipientInvalid", result.Error.Code);
    }

    [Theory]
    [InlineData("ops", true)]
    [InlineData("team-alerts", true)]
    [InlineData("https://evil.test/hook", false)]   // a request names an endpoint, never a URL
    [InlineData("Ops", false)]
    [InlineData("", false)]
    public void Webhook_recipients_are_endpoint_names(string recipient, bool valid) =>
        Assert.Equal(valid, NotificationMessage.IsValidRecipient(NotificationChannel.Webhook, recipient));

    [Theory]
    [InlineData("Welcome")]
    [InlineData("has space")]
    [InlineData("")]
    public void Create_rejects_bad_template_keys(string key)
    {
        var result = NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", key, null, Now);
        Assert.True(result.IsFailure);
        Assert.Equal("Notification.TemplateKeyInvalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_or_oversized_deduplication_keys()
    {
        Assert.True(NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", "", Now).IsFailure);
        Assert.True(NotificationMessage.Create(NotificationChannel.Email, "ann@example.com", "welcome", new string('k', 129), Now).IsFailure);
    }

    [Fact]
    public void MarkSent_finishes_the_message_and_counts_the_attempt()
    {
        var m = NewEmail();
        Assert.True(m.MarkSent("Hello", Now.AddSeconds(1)).IsSuccess);
        Assert.Equal(NotificationStatus.Sent, m.Status);
        Assert.Equal(1, m.Attempts);
        Assert.Equal("Hello", m.Subject);
        Assert.Equal(Now.AddSeconds(1), m.SentAt);
    }

    [Fact]
    public void A_temporary_failure_keeps_it_pending_and_records_the_error()
    {
        var m = NewEmail();
        m.MarkAttemptFailed("smtp down", permanent: false, Now);
        Assert.Equal(NotificationStatus.Pending, m.Status);
        Assert.Equal(1, m.Attempts);
        Assert.Equal("smtp down", m.LastError);
    }

    [Fact]
    public void A_permanent_failure_ends_it_at_once()
    {
        var m = NewEmail();
        m.MarkAttemptFailed("mailbox unknown", permanent: true, Now);
        Assert.Equal(NotificationStatus.Failed, m.Status);
    }

    [Fact]
    public void Running_out_of_attempts_ends_it()
    {
        var m = NewEmail();
        for (var i = 0; i < NotificationMessage.MaxAttempts - 1; i++)
        {
            m.MarkAttemptFailed("down", permanent: false, Now);
            Assert.Equal(NotificationStatus.Pending, m.Status);
        }

        m.MarkAttemptFailed("down", permanent: false, Now);
        Assert.Equal(NotificationStatus.Failed, m.Status);
    }

    [Fact]
    public void A_success_after_failures_clears_the_error()
    {
        var m = NewEmail();
        m.MarkAttemptFailed("down", permanent: false, Now);
        m.MarkSent("Hello", Now);
        Assert.Null(m.LastError);
        Assert.Equal(2, m.Attempts);
    }

    [Fact]
    public void A_finished_message_cannot_change_again()
    {
        var m = NewEmail();
        m.MarkSent("Hello", Now);
        Assert.True(m.MarkSent("Again", Now).IsFailure);
        Assert.True(m.MarkAttemptFailed("late", permanent: true, Now).IsFailure);
        Assert.Equal(NotificationStatus.Sent, m.Status);
        Assert.Equal(1, m.Attempts);
    }

    [Fact]
    public void Long_errors_are_truncated()
    {
        var m = NewEmail();
        m.MarkAttemptFailed(new string('x', 2000), permanent: false, Now);
        Assert.Equal(NotificationMessage.MaxErrorLength, m.LastError!.Length);
    }
}
