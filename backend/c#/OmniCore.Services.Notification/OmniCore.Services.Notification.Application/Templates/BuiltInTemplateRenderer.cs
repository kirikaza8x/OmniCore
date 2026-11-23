namespace OmniCore.Services.Notification.Application.Templates;

using System.Net;
using System.Text.RegularExpressions;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Templates compiled into the service, with <c>{{name}}</c> placeholders. Values are HTML-encoded in HTML bodies, so a
/// user-controlled value (a display name) cannot inject markup. Missing placeholders render as empty text.
/// To add a template: add an entry to <see cref="Templates"/> and a test.
/// </summary>
public sealed partial class BuiltInTemplateRenderer : ITemplateRenderer
{
    private sealed record Template(string Subject, string Html, string Text);

    private static readonly IReadOnlyDictionary<string, Template> Templates = new Dictionary<string, Template>
    {
        ["welcome"] = new(
            "Welcome to {{appName}}",
            "<h2>Welcome to {{appName}}, {{name}}!</h2><p>Your account is ready. You can sign in now.</p>",
            "Welcome to {{appName}}, {{name}}! Your account is ready."),
        ["test"] = new(
            "{{appName}} test notification",
            "<h2>{{appName}} test</h2><p>If you can read this, delivery works. Sent by {{sentBy}}.</p>",
            "{{appName}} test: if you can read this, delivery works. Sent by {{sentBy}}."),
    };

    public static IReadOnlyCollection<string> Keys => Templates.Keys.ToArray();

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}")]
    private static partial Regex Placeholder();

    public Result<RenderedTemplate> Render(
        string templateKey,
        NotificationChannel channel,
        IReadOnlyDictionary<string, string> model)
    {
        if (!Templates.TryGetValue(templateKey, out var template))
        {
            return Result.Failure<RenderedTemplate>(NotificationErrors.TemplateNotFound);
        }

        var html = channel == NotificationChannel.Email;

        // The subject is a header: never HTML-encoded, and line breaks are removed so a value cannot add headers.
        var subject = Fill(template.Subject, model, encode: false).Replace('\r', ' ').Replace('\n', ' ');
        var body = Fill(html ? template.Html : template.Text, model, encode: html);

        return new RenderedTemplate(subject, body, html);
    }

    private static string Fill(string text, IReadOnlyDictionary<string, string> model, bool encode) =>
        Placeholder().Replace(text, match =>
        {
            var value = model.TryGetValue(match.Groups[1].Value, out var v) ? v : string.Empty;
            return encode ? WebUtility.HtmlEncode(value) : value;
        });
}
