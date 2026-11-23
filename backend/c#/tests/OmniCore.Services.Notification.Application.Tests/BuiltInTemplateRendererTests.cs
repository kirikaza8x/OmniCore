using OmniCore.Services.Notification.Application.Templates;
using OmniCore.Services.Notification.Domain.Enums;

namespace OmniCore.Services.Notification.Application.Tests;

public sealed class BuiltInTemplateRendererTests
{
    private readonly BuiltInTemplateRenderer _renderer = new();

    private static Dictionary<string, string> Model(string name) => new() { ["name"] = name, ["appName"] = "OmniCore" };

    [Fact]
    public void Email_gets_an_html_body_with_the_values_filled_in()
    {
        var result = _renderer.Render("welcome", NotificationChannel.Email, Model("Ann"));
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsHtml);
        Assert.Equal("Welcome to OmniCore", result.Value.Subject);
        Assert.Contains("Ann", result.Value.Body);
    }

    [Fact]
    public void Html_bodies_encode_values_so_a_name_cannot_inject_markup()
    {
        var result = _renderer.Render("welcome", NotificationChannel.Email, Model("<script>alert(1)</script>"));
        Assert.DoesNotContain("<script>", result.Value.Body);
        Assert.Contains("&lt;script&gt;", result.Value.Body);
    }

    [Fact]
    public void Webhook_gets_plain_text_that_is_not_encoded()
    {
        var result = _renderer.Render("welcome", NotificationChannel.Webhook, Model("A&B"));
        Assert.False(result.Value.IsHtml);
        Assert.Contains("A&B", result.Value.Body);
    }

    [Fact]
    public void A_value_cannot_add_headers_through_the_subject()
    {
        var model = new Dictionary<string, string> { ["appName"] = "Evil\r\nBcc: x@evil.test" };
        var subject = _renderer.Render("test", NotificationChannel.Email, model).Value.Subject;
        Assert.DoesNotContain('\r', subject);
        Assert.DoesNotContain('\n', subject);
    }

    [Fact]
    public void Missing_placeholders_render_as_empty_text()
    {
        var result = _renderer.Render("welcome", NotificationChannel.Webhook, new Dictionary<string, string>());
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("{{", result.Value.Body);
    }

    [Fact]
    public void An_unknown_template_is_a_validation_error()
    {
        var result = _renderer.Render("nope", NotificationChannel.Email, Model("Ann"));
        Assert.True(result.IsFailure);
        Assert.Equal("Notification.TemplateNotFound", result.Error.Code);
    }
}
