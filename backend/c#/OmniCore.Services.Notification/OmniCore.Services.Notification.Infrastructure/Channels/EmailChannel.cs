namespace OmniCore.Services.Notification.Infrastructure.Channels;

using MailKit;
using MailKit.Net.Smtp;
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
        catch (SmtpCommandException ex) when ((int)ex.StatusCode is >= 500 and < 600 && (int)ex.StatusCode != 535)
        {
            // 5xx = the server refused this message (mailbox unknown, rejected). Trying again will not change that.
            // 535 (authentication failed) is a configuration problem: it stays retryable, so the message waits in the
            // error queue until the credentials are fixed.
            return DeliveryResult.Permanent($"SMTP {(int)ex.StatusCode}: {ex.Message}");
        }
        catch (ParseException ex)
        {
            return DeliveryResult.Permanent($"Invalid address: {ex.Message}");
        }
        catch (Exception ex) when (ex is ServiceNotConnectedException or ServiceNotAuthenticatedException or SmtpProtocolException
                                       or IOException or System.Net.Sockets.SocketException or TimeoutException
                                       or OperationCanceledException or SmtpCommandException or ProtocolException)
        {
            logger.LogWarning(ex, "E-mail delivery failed temporarily");
            return DeliveryResult.Transient($"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
