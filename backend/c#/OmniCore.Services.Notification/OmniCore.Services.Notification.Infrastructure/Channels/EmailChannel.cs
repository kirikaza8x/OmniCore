namespace OmniCore.Services.Notification.Infrastructure.Channels;

using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Application.Abstractions.Notifications;

/// <summary>Delivers by e-mail through the shared <see cref="IEmailSender"/> (SMTP in every environment; Mailpit in dev).</summary>
public sealed class EmailChannel(IEmailSender sender, ILogger<EmailChannel> logger) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<DeliveryResult> SendAsync(OutgoingNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await sender.SendAsync(
                new EmailMessage(notification.Recipient, notification.Subject, notification.Body, notification.IsHtml),
                cancellationToken);
            return DeliveryResult.Ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // shutting down: let the broker redeliver
        }
        catch (SmtpCommandException ex) when ((int)ex.StatusCode is >= 500 and < 600)
        {
            // 5xx = the server refused this message (mailbox unknown, rejected). Trying again will not change that.
            return DeliveryResult.Permanent($"SMTP {(int)ex.StatusCode}: {ex.Message}");
        }
        catch (ParseException ex)
        {
            return DeliveryResult.Permanent($"Invalid address: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Connection refused, timeout, TLS or login problem (a wrong password is fixed by configuration, so it stays
            // retryable: the message waits in the error queue), 4xx replies. Trying again can help.
            logger.LogWarning(ex, "E-mail delivery failed temporarily");
            return DeliveryResult.Transient($"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
