namespace OmniCore.Shared.Application.Abstractions.Authentication;

using OmniCore.Shared.Application.DTOs;

/// <summary>
/// Provides methods to detect and generate device information for multi-device session support.
/// </summary>
public interface IDeviceDetectionService
{
    /// <summary>
    /// Extracts device information from the provided user agent string and optional existing device ID.
    /// </summary>
    DeviceInfo GetDeviceInfo(string? userAgent, string? ipAddress = null, string? existingDeviceId = null);

    /// <summary>
    /// Generates a new unique device identifier.
    /// </summary>
    string GenerateDeviceId();

    /// <summary>
    /// Attempts to resolve a friendly device name (e.g., "iPhone 14", "Windows PC") from the user agent.
    /// </summary>
    string? ResolveDeviceName(string? userAgent);
}