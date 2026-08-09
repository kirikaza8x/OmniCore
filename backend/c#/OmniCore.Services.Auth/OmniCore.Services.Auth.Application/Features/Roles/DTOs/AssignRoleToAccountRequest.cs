namespace OmniCore.Services.Auth.Application.Features.Roles.DTOs;


public record AssignRoleToAccountRequest(Guid AccountId, Guid RoleId);

