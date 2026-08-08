namespace OmniCore.Services.Auth.Application.Features.Auth.DTOs;

public record AccountDto(
    Guid Id,
    string? Email,
    string Username,
    string? PasswordHash,
    bool IsActive,
    List<string> Roles);