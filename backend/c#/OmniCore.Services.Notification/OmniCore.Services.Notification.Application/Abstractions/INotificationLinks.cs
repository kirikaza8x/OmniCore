namespace OmniCore.Services.Notification.Application.Abstractions;

/// <summary>Builds the links that go into account e-mails, pointing at the web client (ADR 0009).</summary>
public interface INotificationLinks
{
    /// <summary>Link to the page where the user chooses a new password. The token is escaped.</summary>
    string ResetPassword(string token);

    /// <summary>Link to the page that confirms the e-mail address. The token is escaped.</summary>
    string VerifyEmail(string token);
}
