namespace OmniCore.Services.Auth.Api.Routing;

using OmniCore.Shared.Api.Routing;

public sealed class AuthRoutes : BaseRouteModule
{
    // Sets the service level prefix (/api/v1/auth)
    protected override string? ServiceName => "auth";

    // Standard auth actions operate at the service root level, so ResourceName is null
    protected override string? ResourceName => null;

    // Sub-paths
    public string Register => "register";
    public string Login => "login";
    public string RefreshToken => "refresh-token";
    public string Logout => "logout";

    public string Me => "me";

    // Password reset and e-mail verification (ADR 0009)
    public string ForgotPassword => "password/forgot";
    public string ResetPassword => "password/reset";
    public string RequestEmailVerification => "email/verify/request";
    public string ConfirmEmail => "email/verify/confirm";
    public string ResendEmailVerification => "email/verify/resend";
    public string AdminConfirmEmails => "admin/emails/confirm";

    // Tenants (ADR 0001)
    public string MyTenants => "tenants";
    public string SwitchTenant => "switch-tenant";
    public string AdminTenants => "admin/tenants";
    public string AdminTenantMembers => "admin/tenants/{tenantId:guid}/members";
}