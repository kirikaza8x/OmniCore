namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.CreateRole;

using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record CreateRoleCommand(
    string Name, 
    string? Description = null) : ICommand<RoleResponse>;