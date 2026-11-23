namespace OmniCore.Services.Notification.Contracts.Events;

using OmniCore.Shared.Contracts.Events;

/// <summary>
/// Any service can publish this to have Notification deliver something, without knowing about SMTP or webhooks.
/// <para><c>Channel</c> is a string ("Email", "Webhook") so adding a channel is not a breaking change.
/// <c>Recipient</c> is an e-mail address, or for Webhook the configured endpoint name.
/// <c>Model</c> fills the <c>{{placeholders}}</c> of the template. <c>DeduplicationKey</c> makes a redelivery harmless;
/// when null the event id is used.</para>
/// </summary>
public record NotificationRequestedIntegrationEvent(
    string Channel,
    string Recipient,
    string TemplateKey,
    IReadOnlyDictionary<string, string> Model,
    string? DeduplicationKey = null) : IntegrationEvent;
