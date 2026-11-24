namespace OmniCore.Services.Auth.Application.Abstractions.Security;

/// <summary>Switches that change who may sign in (ADR 0010). Read from configuration, so they are the same for every request.</summary>
public interface IAuthPolicy
{
    /// <summary>When true, only accounts with a confirmed e-mail can sign in.</summary>
    bool RequireVerifiedEmail { get; }
}
