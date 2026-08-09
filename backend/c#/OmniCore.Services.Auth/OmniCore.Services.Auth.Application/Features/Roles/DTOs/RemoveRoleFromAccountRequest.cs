namespace OmniCore.Services.Auth.Application.Features.Roles.DTOs;


public record RemoveRoleFromAccountRequest(Guid AccountId, Guid RoleId);