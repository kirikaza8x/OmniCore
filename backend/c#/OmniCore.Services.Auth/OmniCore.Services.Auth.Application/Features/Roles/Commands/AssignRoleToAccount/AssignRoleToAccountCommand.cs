using OmniCore.Shared.Application.Abstractions.Messaging;

namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.AssignRoleToAccount;


public record AssignRoleToAccountCommand(Guid AccountId, Guid RoleId) : ICommand;