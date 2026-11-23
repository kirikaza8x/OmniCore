namespace OmniCore.Services.Notification.Domain.Errors;

using OmniCore.Shared.Domain.Abstractions;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Notification.NotFound", "The requested notification was not found.");

    public static readonly Error RecipientInvalid =
        Error.Validation("Notification.RecipientInvalid", "The recipient is not valid for this channel.");

    public static readonly Error TemplateKeyInvalid =
        Error.Validation("Notification.TemplateKeyInvalid", "Template keys use lower-case letters, digits, '.' and '-' (max 64).");

    public static readonly Error DeduplicationKeyInvalid =
        Error.Validation("Notification.DeduplicationKeyInvalid", "The deduplication key must be 1-128 characters.");

    public static readonly Error TemplateNotFound =
        Error.Validation("Notification.TemplateNotFound", "There is no template with that key.");

    public static readonly Error ChannelNotAvailable =
        Error.Validation("Notification.ChannelNotAvailable", "That channel is not available.");

    public static readonly Error AlreadyFinished =
        Error.Conflict("Notification.AlreadyFinished", "The notification was already sent or has permanently failed.");

    /// <summary>Temporary: the caller (or the broker) should try again.</summary>
    public static readonly Error DeliveryFailed =
        Error.Failure("Notification.DeliveryFailed", "The notification could not be delivered; it will be retried.");
}
