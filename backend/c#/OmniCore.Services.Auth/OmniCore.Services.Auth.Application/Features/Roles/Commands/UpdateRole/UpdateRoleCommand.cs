namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.UpdateRole;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record UpdateRoleCommand(Guid RoleId, string NewName) : ICommand<RoleResponse>;