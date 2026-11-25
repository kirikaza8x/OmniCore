namespace OmniCore.Services.Auth.Application.Features.Tenants.Commands;

using FluentValidation;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Issues a new access token for another tenant the caller is a member of (ADR 0001). The refresh token stays bound
/// to the account and is not touched. The tenant is checked against the membership, never trusted from the client.
/// </summary>
public record SwitchTenantCommand(Guid TenantId) : ICommand<SwitchTenantResponse>;

public record SwitchTenantResponse(string AccessToken, int ExpiresInMinutes, TenantDto Tenant);

public sealed class SwitchTenantCommandValidator : AbstractValidator<SwitchTenantCommand>
{
    public SwitchTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEqual(Guid.Empty).WithMessage("Tenant id is required.");
    }
}

public sealed class SwitchTenantCommandHandler(
    IAccountRepository accounts,
    ITenantRepository tenants,
    IJwtTokenService jwt,
    ICurrentUserService currentUser) : ICommandHandler<SwitchTenantCommand, SwitchTenantResponse>
{
    internal static readonly Error NotAMember =
        Error.Forbidden("Tenant.NotAMember", "You are not a member of this tenant.");

    public async Task<Result<SwitchTenantResponse>> Handle(SwitchTenantCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure<SwitchTenantResponse>(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        var accountId = new AccountId(userId);
        var account = await accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure<SwitchTenantResponse>(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        // Same answer for "no such tenant", "inactive tenant" and "not a member": nothing to learn about other tenants.
        var memberships = await tenants.GetMembershipsAsync(accountId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.TenantId.Value == request.TenantId);
        if (membership is null)
        {
            return Result.Failure<SwitchTenantResponse>(NotAMember);
        }

        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        var token = jwt.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles,
            TenantSelector.ToClaim(membership));

        return Result.Success(new SwitchTenantResponse(
            token,
            jwt.ExpiryMinutes,
            new TenantDto(membership.TenantId.Value, membership.Slug, membership.Name, membership.Role)));
    }
}
