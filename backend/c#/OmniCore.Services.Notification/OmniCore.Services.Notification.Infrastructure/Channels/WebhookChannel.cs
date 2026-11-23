namespace OmniCore.Services.Notification.Infrastructure.Channels;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Infrastructure.Configs;

/// <summary>
/// POSTs JSON to a configured endpoint: <c>{"subject", "text", "content"}</c>. <c>text</c> is what Slack-style hooks read and
/// <c>content</c> what Discord-style hooks read (both carry subject and body), so most incoming webhooks work unchanged.
/// </summary>
public sealed class WebhookChannel(
    IHttpClientFactory httpClientFactory,
    IOptions<WebhooksConfig> options,
    ILogger<WebhookChannel> logger) : INotificationChannel
{
    public const string HttpClientName = "notification-webhook";

    public NotificationChannel Channel => NotificationChannel.Webhook;

    public async Task<DeliveryResult> SendAsync(OutgoingNotification notification, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (!config.Endpoints.TryGetValue(notification.Recipient, out var url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return DeliveryResult.Permanent($"No webhook endpoint named '{notification.Recipient}' is configured.");
        }

        var text = $"{notification.Subject}\n{notification.Body}";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 1, 60)));

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(
                uri, new { subject = notification.Subject, text, content = text }, timeout.Token);

            if (response.IsSuccessStatusCode)
            {
                return DeliveryResult.Ok;
            }

            // The URL is a secret: only the name and the status are logged.
            var status = (int)response.StatusCode;
            logger.LogWarning("Webhook '{Endpoint}' answered {Status}", notification.Recipient, status);
            var retryable = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || status >= 500;
            return retryable
                ? DeliveryResult.Transient($"Webhook answered {status}")
                : DeliveryResult.Permanent($"Webhook answered {status}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            logger.LogWarning("Webhook '{Endpoint}' unreachable: {Type}", notification.Recipient, ex.GetType().Name);
            return DeliveryResult.Transient($"Webhook unreachable ({ex.GetType().Name})");
        }
    }
}
