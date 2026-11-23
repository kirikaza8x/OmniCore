namespace OmniCore.Services.Notification.Application.Features.Notifications.IntegrationEvents;

using Microsoft.Extensions.Logging;
using OmniCore.Shared.Domain.Abstractions;

internal static class NotificationEventOutcome
{
    /// <summary>
    /// Throwing makes MassTransit retry and then park the message in the error queue. Only a temporary failure should do
    /// that; a request that can never succeed (bad address, unknown template) is logged and dropped.
    /// </summary>
    public static void ThrowIfRetryable(Result result, ILogger logger, string eventName, Guid eventId)
    {
        if (result.IsSuccess)
        {
            return;
        }

        if (result.Error.Type is ErrorType.Validation or ErrorType.NotFound or ErrorType.Conflict)
        {
            logger.LogWarning("{Event} {EventId} dropped: {Code}", eventName, eventId, result.Error.Code);
            return;
        }

        throw new InvalidOperationException($"{eventName} {eventId} could not be delivered yet: {result.Error.Code}");
    }
}
