namespace OmniCore.Services.Auth.Application.Features.Auth.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes
);