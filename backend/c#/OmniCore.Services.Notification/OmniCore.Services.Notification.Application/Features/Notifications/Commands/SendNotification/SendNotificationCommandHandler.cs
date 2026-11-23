namespace OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;

using Microsoft.Extensions.Logging;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Services.Notification.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.Repositories;

public sealed class SendNotificationCommandHandler(
    INotificationMessageRepository messages,
    IEnumerable<INotificationChannel> channels,
    ITemplateRenderer renderer,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<SendNotificationCommandHandler> logger)
    : ICommandHandler<SendNotificationCommand, NotificationResponse>
{
    public async Task<Result<NotificationResponse>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        // A redelivered request continues the same message; one that is already finished is not sent again.
        NotificationMessage? message = null;
        if (request.DeduplicationKey is not null)
        {
            message = await messages.GetByDeduplicationKeyAsync(request.DeduplicationKey, cancellationToken);
            if (message is { Status: not NotificationStatus.Pending })
            {
                return message.ToResponse();
            }
        }

        var channel = channels.FirstOrDefault(c => c.Channel == request.Channel);
        if (channel is null)
        {
            return Result.Failure<NotificationResponse>(NotificationErrors.ChannelNotAvailable);
        }

        var model = request.Model ?? new Dictionary<string, string>();
        var rendered = renderer.Render(request.TemplateKey, request.Channel, model);
        if (rendered.IsFailure)
        {
            return Result.Failure<NotificationResponse>(rendered.Error);
        }

        if (message is null)
        {
            var created = NotificationMessage.Create(
                request.Channel, request.Recipient, request.TemplateKey, request.DeduplicationKey, now);
            if (created.IsFailure)
            {
                return Result.Failure<NotificationResponse>(created.Error);
            }

            message = created.Value;
            messages.Add(message);
        }

        var outgoing = new OutgoingNotification(message.Recipient, rendered.Value.Subject, rendered.Value.Body, rendered.Value.IsHtml);
        var outcome = await channel.SendAsync(outgoing, cancellationToken);

        if (outcome.Succeeded)
        {
            message.MarkSent(rendered.Value.Subject, now);
            return message.ToResponse(); // saved by the unit-of-work behaviour (success result)
        }

        message.MarkAttemptFailed(outcome.Error ?? "Delivery failed.", outcome.IsPermanent, now);
        logger.LogWarning(
            "Notification {NotificationId} on {Channel}, attempt {Attempts}: {Status} ({Error})",
            message.Id.Value, message.Channel, message.Attempts, message.Status, message.LastError);

        // The behaviour saves only on success, and the failed attempt must be kept either way (CLAUDE.md).
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return message.Status == NotificationStatus.Failed
            ? message.ToResponse()                                   // gave up: nothing left to retry
            : Result.Failure<NotificationResponse>(NotificationErrors.DeliveryFailed); // caller retries
    }
}
