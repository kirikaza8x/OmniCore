namespace OmniCore.Shared.Infrastructure.Services.Authentication;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.DTOs;
using OmniCore.Shared.Infrastructure.Middleware;

/// <summary>
/// Service providing access to the currently authenticated user's contextual information, claims, and device data.
/// </summary>
public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDeviceDetectionService _deviceDetectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the active HTTP context.</param>
    /// <param name="deviceDetectionService">Service for parsing client device information.</param>
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IDeviceDetectionService deviceDetectionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _deviceDetectionService = deviceDetectionService;
    }

    private HttpContext? Context => _httpContextAccessor.HttpContext;
    private ClaimsPrincipal? User => Context?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var value = GetClaimValue(ClaimTypes.NameIdentifier) ?? GetClaimValue("sub");
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }

    /// <inheritdoc />
    public string? Email => GetClaimValue(ClaimTypes.Email) ?? GetClaimValue("email");

    /// <inheritdoc />
    public string? Name => GetClaimValue(ClaimTypes.Name) ?? GetClaimValue("name");

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
    User?.FindAll(c => c.Type == ClaimTypes.Role || c.Type == "role")
    .Select(c => c.Value)
    .Where(role => !string.IsNullOrWhiteSpace(role))
    .Distinct()
    .ToList()
     ?? new List<string>();


    /// <inheritdoc />
    public string? Jti => GetClaimValue("jti") ?? GetClaimValue(ClaimTypes.SerialNumber);

    /// <inheritdoc />
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();

    /// <inheritdoc />
    public string? DeviceId => GetDeviceIdFromContext() ?? GetDeviceIdFromHeader();

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public CurrentUserDto GetCurrentUser()
    {
        var deviceInfo = _deviceDetectionService.GetDeviceInfo(UserAgent, IpAddress, DeviceId);

        return new CurrentUserDto
        {
            UserId = UserId ?? Guid.Empty,
            Email = Email,
            Name = Name,
            Roles = Roles,
            Jti = Jti,
            IpAddress = IpAddress,
            Device = deviceInfo,
            UserAgent = UserAgent
        };
    }

    private string? GetClaimValue(string claimType)
    {
        return User?.FindFirst(claimType)?.Value;
    }

    private string? GetDeviceIdFromContext()
    {
        return Context?.Items[DeviceIdMiddleware.ItemKey] as string;
    }

    private string? GetDeviceIdFromHeader()
    {
        return Context?.Request.Headers[DeviceIdMiddleware.HeaderName].ToString();
    }
}