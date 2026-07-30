namespace OmniCore.Shared.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task SendAsync(
        EmailMessage message, 
        CancellationToken cancellationToken = default);

    Task SendBatchAsync(
        IEnumerable<EmailMessage> messages, 
        CancellationToken cancellationToken = default);
}