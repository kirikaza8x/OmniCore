namespace OmniCore.Services.Notification.Domain.Entities;

using System.Net.Mail;
using System.Text.RegularExpressions;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Services.Notification.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// One delivery attempt chain to one recipient on one channel. It is the delivery log: who, which template, what happened.
/// The rendered body and the template model are deliberately NOT stored (they will carry reset links and tokens).
/// </summary>
public class NotificationMessage : AggregateRoot<NotificationMessageId>
{
    public const int MaxAttempts = 5;
    public const int MaxRecipientLength = 256;
    public const int MaxErrorLength = 500;
    public const int MaxDeduplicationKeyLength = 128;

    /// <summary>How long a pending first attempt stays owned by the consumer that claimed it.</summary>
    public static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(1);

    private static readonly Regex TemplateKeyRegex = new("^[a-z0-9][a-z0-9.-]{0,63}$", RegexOptions.Compiled);
    private static readonly Regex WebhookNameRegex = new("^[a-z0-9][a-z0-9-]{0,49}$", RegexOptions.Compiled);

    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string TemplateKey { get; private set; } = null!;
    public string? Subject { get; private set; }
    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    /// <summary>Same key = same notification: a redelivered request does not send twice. Null means "never deduplicate".</summary>
    public string? DeduplicationKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationMessage() { }

    private NotificationMessage(NotificationMessageId id) : base(id) { }

    public static Result<NotificationMessage> Create(
        NotificationChannel channel,
        string recipient,
        string templateKey,
        string? deduplicationKey,
        DateTime nowUtc)
    {
        recipient = recipient?.Trim() ?? string.Empty;
        if (!IsValidRecipient(channel, recipient))
        {
            return Result.Failure<NotificationMessage>(NotificationErrors.RecipientInvalid);
        }

        if (templateKey is null || !TemplateKeyRegex.IsMatch(templateKey))
        {
            return Result.Failure<NotificationMessage>(NotificationErrors.TemplateKeyInvalid);
        }

        if (deduplicationKey is not null && (deduplicationKey.Length is 0 or > MaxDeduplicationKeyLength))
        {
            return Result.Failure<NotificationMessage>(NotificationErrors.DeduplicationKeyInvalid);
        }

        return new NotificationMessage(NotificationMessageId.New())
        {
            Channel = channel,
            Recipient = recipient,
            TemplateKey = templateKey,
            DeduplicationKey = deduplicationKey,
            Status = NotificationStatus.Pending,
            CreatedAt = nowUtc,
        };
    }

    public static bool IsValidRecipient(NotificationChannel channel, string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient) || recipient.Length > MaxRecipientLength)
        {
            return false;
        }

        return channel switch
        {
            // MailAddress alone accepts display names and comments, so the parsed address must equal the input.
            NotificationChannel.Email => MailAddress.TryCreate(recipient, out var mail) && mail.Address == recipient,
            NotificationChannel.Webhook => WebhookNameRegex.IsMatch(recipient),
            _ => false,
        };
    }

    /// <summary>
    /// True while another consumer owns a fresh claim: it inserted the row but has not made a send attempt yet.
    /// A duplicate event in that window must not send a second mail (CI run #66).
    /// </summary>
    public bool HasActiveClaim(DateTime nowUtc) =>
        Status == NotificationStatus.Pending && Attempts == 0 && nowUtc - CreatedAt < ClaimLease;

    public Result MarkSent(string subject, DateTime nowUtc)
    {
        if (Status != NotificationStatus.Pending)
        {
            return Result.Failure(NotificationErrors.AlreadyFinished);
        }

        Attempts++;
        Subject = subject;
        Status = NotificationStatus.Sent;
        LastError = null;
        LastAttemptAt = nowUtc;
        SentAt = nowUtc;
        return Result.Success();
    }

    /// <summary>Records a failed attempt. A permanent error, or the attempt limit, ends it as <c>Failed</c>.</summary>
    public Result MarkAttemptFailed(string error, bool permanent, DateTime nowUtc)
    {
        if (Status != NotificationStatus.Pending)
        {
            return Result.Failure(NotificationErrors.AlreadyFinished);
        }

        Attempts++;
        LastAttemptAt = nowUtc;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        if (permanent || Attempts >= MaxAttempts)
        {
            Status = NotificationStatus.Failed;
        }

        return Result.Success();
    }
}
