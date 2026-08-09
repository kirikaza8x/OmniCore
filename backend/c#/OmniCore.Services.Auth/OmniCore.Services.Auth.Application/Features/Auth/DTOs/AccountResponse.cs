namespace OmniCore.Services.Auth.Application.Features.Auth.DTOs;

public record AccountResponse(
    Guid Id,
    string? Email,
    string Username,
    bool IsActive,
    List<string> Roles);