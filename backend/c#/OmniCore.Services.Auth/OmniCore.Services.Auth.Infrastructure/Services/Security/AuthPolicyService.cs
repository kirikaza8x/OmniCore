namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Infrastructure.Configs;

public sealed class AuthPolicyService(IOptions<AuthPolicyConfig> options) : IAuthPolicy
{
    public bool RequireVerifiedEmail => options.Value.RequireVerifiedEmail;
}
