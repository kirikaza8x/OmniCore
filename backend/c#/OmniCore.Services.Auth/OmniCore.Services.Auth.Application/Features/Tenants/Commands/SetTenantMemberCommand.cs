namespace OmniCore.Services.Auth.Application.Features.Tenants.Commands;

using FluentValidation;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

/// <summary>Platform admin adds an account to a tenant, or changes its role there. TenantId comes from the route.</summary>
public record SetTenantMemberCommand(Guid TenantId, Guid AccountId, string Role) : ICommand<TenantMemberResponse>;

public record TenantMemberResponse(Guid TenantId, Guid AccountId, string Role, bool Created);

public sealed class SetTenantMemberCommandValidator : AbstractValidator<SetTenantMemberCommand>
{
    public SetTenantMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEqual(Guid.Empty);
        RuleFor(x => x.AccountId).NotEqual(Guid.Empty);
        RuleFor(x => x.Role).Must(TenantRoles.IsValid)
            .WithMessage($"Role must be one of: {string.Join(", ", TenantRoles.All)}.");
    }
}

public sealed class SetTenantMemberCommandHandler(
    ITenantRepository tenants,
    IAccountRepository accounts,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    ILogger<SetTenantMemberCommandHandler> logger) : ICommandHandler<SetTenantMemberCommand, TenantMemberResponse>
{
    public async Task<Result<TenantMemberResponse>> Handle(SetTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var accountId = new AccountId(request.AccountId);

        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantMemberResponse>(Error.NotFound("Tenant.NotFound", "Tenant not found."));
        }

        var existing = await tenants.GetMembershipAsync(tenantId, accountId, cancellationToken);
        if (existing is not null)
        {
            var changed = existing.ChangeRole(request.Role);
            if (changed.IsFailure)
            {
                return Result.Failure<TenantMemberResponse>(changed.Error);
            }

            logger.LogInformation("Admin {AdminId} set role {Role} for account {AccountId} in tenant {TenantId}",
                currentUser.UserId, request.Role, request.AccountId, request.TenantId);
            return Result.Success(new TenantMemberResponse(request.TenantId, request.AccountId, request.Role, Created: false));
        }

        var account = await accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<TenantMemberResponse>(Error.NotFound("Account.NotFound", "Account not found."));
        }

        var membership = TenantMembership.Create(tenantId, accountId, request.Role, clock.UtcNow);
        if (membership.IsFailure)
        {
            return Result.Failure<TenantMemberResponse>(membership.Error);
        }

        tenants.Add(membership.Value);

        logger.LogInformation("Admin {AdminId} added account {AccountId} to tenant {TenantId} as {Role}",
            currentUser.UserId, request.AccountId, request.TenantId, request.Role);
        return Result.Success(new TenantMemberResponse(request.TenantId, request.AccountId, request.Role, Created: true));
    }
}
