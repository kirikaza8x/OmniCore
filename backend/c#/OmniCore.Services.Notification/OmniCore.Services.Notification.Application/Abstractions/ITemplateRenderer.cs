namespace OmniCore.Services.Notification.Application.Abstractions;

using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>Turns a template key and a model into subject and body for a channel. Swap the implementation to read templates from a database or a file store.</summary>
public interface ITemplateRenderer
{
    Result<RenderedTemplate> Render(string templateKey, NotificationChannel channel, IReadOnlyDictionary<string, string> model);
}

public sealed record RenderedTemplate(string Subject, string Body, bool IsHtml);
