namespace OmniCore.Shared.Application.Abstractions.Notifications;

public sealed record EmailMessage
{
    public IReadOnlyList<string> To { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
    public bool IsHtml { get; init; }
    public IReadOnlyList<string>? Cc { get; init; }
    public IReadOnlyList<string>? Bcc { get; init; }

    public EmailMessage(
        string to, 
        string subject, 
        string body, 
        bool isHtml = true)
    {
        To = new[] { to };
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }

    public EmailMessage(
        IEnumerable<string> to, 
        string subject, 
        string body, 
        bool isHtml = true)
    {
        To = to.ToList().AsReadOnly();
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }
}