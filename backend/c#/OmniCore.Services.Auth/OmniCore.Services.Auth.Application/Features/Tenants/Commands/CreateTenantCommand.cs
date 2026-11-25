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

/// <summary>Platform admin creates a tenant (ADR 0001: tenants are admin-created). The caller becomes its owner.</summary>
public record CreateTenantCommand(string Slug, string Name) : ICommand<TenantDto>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(63);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateTenantCommandHandler(
    ITenantRepository tenants,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    ILogger<CreateTenantCommandHandler> logger) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } adminId || adminId == Guid.Empty)
        {
            return Result.Failure<TenantDto>(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        var created = Tenant.Create(request.Slug, request.Name, clock.UtcNow);
        if (created.IsFailure)
        {
            return Result.Failure<TenantDto>(created.Error);
        }

        var tenant = created.Value;
        if (await tenants.SlugExistsAsync(tenant.Slug, cancellationToken))
        {
            return Result.Failure<TenantDto>(Error.Conflict("Tenant.SlugTaken", "A tenant with this slug already exists."));
        }

        var membership = TenantMembership.Create(tenant.Id, new AccountId(adminId), TenantRoles.Owner, clock.UtcNow);
        if (membership.IsFailure)
        {
            return Result.Failure<TenantDto>(membership.Error);
        }

        tenants.Add(tenant);
        tenants.Add(membership.Value);

        logger.LogInformation("Tenant {Slug} ({TenantId}) created by {AdminId}", tenant.Slug, tenant.Id, adminId);

        return Result.Success(new TenantDto(tenant.Id.Value, tenant.Slug, tenant.Name, TenantRoles.Owner));
    }
}
