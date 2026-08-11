namespace OmniCore.Services.Auth.Application.Features.Roles.DTOs;

public record CreateRoleRequest(string Name, string? Description = null);