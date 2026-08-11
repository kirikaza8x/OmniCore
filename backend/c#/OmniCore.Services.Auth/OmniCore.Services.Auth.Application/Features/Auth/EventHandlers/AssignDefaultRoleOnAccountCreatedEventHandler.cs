namespace OmniCore.Services.Auth.Application.Features.Auth.EventHandlers;

using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;

public sealed class AssignDefaultRoleOnAccountCreatedEventHandler(
    IAccountRepository accountRepository,
    IRoleRepository roleRepository,
    ILogger<AssignDefaultRoleOnAccountCreatedEventHandler> logger) 
    : IDomainEventHandler<AccountCreatedDomainEvent>
{
    private const string DefaultRoleName = "User";

    public async Task HandleAsync(
        AccountCreatedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling AccountCreatedDomainEvent for Account ID: {AccountId}", domainEvent.AccountId.Value);

        var account = accountRepository.GetLocalById(domainEvent.AccountId) 
                   ?? await accountRepository.GetByIdAsync(domainEvent.AccountId, cancellationToken);

        if (account is null)
        {
            logger.LogWarning("Account {AccountId} was NOT found in local tracker or DB!", domainEvent.AccountId.Value);
            return;
        }

        var defaultRole = await roleRepository.GetByNameAsync(DefaultRoleName, cancellationToken);
        if (defaultRole is null)
        {
            logger.LogError("Default Role '{RoleName}' was NOT found in DB!", DefaultRoleName);
            return;
        }

        var result = account.AssignRole(defaultRole.Id);
        if (result.IsSuccess)
        {
            logger.LogInformation("Successfully assigned default role '{RoleName}' to Account ID: {AccountId}", DefaultRoleName, domainEvent.AccountId.Value);
        }
        else
        {
            logger.LogWarning("Failed to assign default role: {Error}", result.Error.Description);
        }
    }
}