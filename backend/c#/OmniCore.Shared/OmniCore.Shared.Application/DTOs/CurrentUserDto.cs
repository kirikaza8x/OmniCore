namespace OmniCore.Shared.Application.DTOs;

public record CurrentUserDto
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string? Jti { get; init; }
    public string? IpAddress { get; init; }
    public DeviceInfo? Device { get; init; }
    public string? UserAgent { get; init; }
}