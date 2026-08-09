using OmniCore.Shared.Application.Abstractions.Messaging;

namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.RemoveRoleFromAccount;


public record RemoveRoleFromAccountCommand(Guid AccountId, Guid RoleId) : ICommand;


