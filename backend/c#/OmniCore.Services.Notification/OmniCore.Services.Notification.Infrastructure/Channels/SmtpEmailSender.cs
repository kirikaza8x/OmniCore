namespace OmniCore.Services.Notification.Infrastructure.Channels;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OmniCore.Services.Notification.Infrastructure.Configs;
using OmniCore.Shared.Application.Abstractions.Notifications;

/// <summary>MailKit implementation of the shared <see cref="IEmailSender"/>. Throws on failure; <see cref="EmailChannel"/> classifies the error.</summary>
public sealed class SmtpEmailSender(IOptions<SmtpConfig> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        using var mime = Build(message, config);
        using var client = new SmtpClient { Timeout = config.TimeoutSeconds * 1000 };

        await client.ConnectAsync(config.Host, config.Port, ParseSecurity(config.Security), cancellationToken);
        if (!string.IsNullOrEmpty(config.Username))
        {
            await client.AuthenticateAsync(config.Username, config.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    public async Task SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            await SendAsync(message, cancellationToken);
        }
    }

    internal static MimeMessage Build(EmailMessage message, SmtpConfig config)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        foreach (var to in message.To) mime.To.Add(MailboxAddress.Parse(to));
        foreach (var cc in message.Cc ?? Array.Empty<string>()) mime.Cc.Add(MailboxAddress.Parse(cc));
        foreach (var bcc in message.Bcc ?? Array.Empty<string>()) mime.Bcc.Add(MailboxAddress.Parse(bcc));
        mime.Subject = message.Subject;

        mime.Body = message.IsHtml
            ? new BodyBuilder { HtmlBody = message.Body }.ToMessageBody()
            : new TextPart("plain") { Text = message.Body };
        return mime;
    }

    internal static SecureSocketOptions ParseSecurity(string value) =>
        Enum.TryParse<SecureSocketOptions>(value, ignoreCase: true, out var parsed) ? parsed : SecureSocketOptions.Auto;
}
