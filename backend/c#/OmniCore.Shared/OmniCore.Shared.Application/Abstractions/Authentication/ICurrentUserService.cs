namespace OmniCore.Shared.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    string? Jti { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? DeviceId { get; }

    bool IsInRole(string role);
}