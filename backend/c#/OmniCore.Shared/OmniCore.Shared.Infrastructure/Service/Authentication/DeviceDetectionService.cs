namespace OmniCore.Shared.Infrastructure.Services.Authentication;

using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.DTOs;
using UAParser;

/// <summary>
/// Service responsible for parsing HTTP User-Agent headers to resolve device, browser, and operating system details.
/// </summary>
public sealed class DeviceDetectionService : IDeviceDetectionService
{
    private static readonly Parser UaParser = Parser.GetDefault();

    /// <inheritdoc />
    public DeviceInfo GetDeviceInfo(
        string? userAgent,
        string? ipAddress = null,
        string? existingDeviceId = null)
    {
        var deviceId = string.IsNullOrWhiteSpace(existingDeviceId)
            ? GenerateDeviceId()
            : existingDeviceId;

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new DeviceInfo(
                DeviceId: deviceId,
                DeviceName: "Unknown Device",
                OperatingSystem: "Unknown",
                Browser: "Unknown",
                IpAddress: ipAddress);
        }

        // OPTIMIZATION: Parse User-Agent ONCE and reuse across helpers
        var clientInfo = UaParser.Parse(userAgent);
        var browser = GetBrowserName(clientInfo);
        var os = GetOperatingSystemName(clientInfo);
        var deviceName = ResolveDeviceNameFromClientInfo(clientInfo, browser, os);

        return new DeviceInfo(
            DeviceId: deviceId,
            DeviceName: deviceName,
            OperatingSystem: os,
            Browser: browser,
            IpAddress: ipAddress);
    }

    /// <inheritdoc />
    public string GenerateDeviceId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc />
    public string? ResolveDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown Device";

        var clientInfo = UaParser.Parse(userAgent);
        var browser = GetBrowserName(clientInfo);
        var os = GetOperatingSystemName(clientInfo);

        return ResolveDeviceNameFromClientInfo(clientInfo, browser, os);
    }

    private static string ResolveDeviceNameFromClientInfo(ClientInfo clientInfo, string browser, string os)
    {
        var deviceFamily = clientInfo.Device.Family;

        if (!string.IsNullOrWhiteSpace(deviceFamily) && deviceFamily != "Other")
        {
            return $"{deviceFamily} ({browser} on {os})";
        }

        return $"{browser} on {os}";
    }

    private static string GetBrowserName(ClientInfo clientInfo)
    {
        var browser = clientInfo.UA.Family;
        return string.IsNullOrWhiteSpace(browser) ? "Unknown Browser" : browser;
    }

    private static string GetOperatingSystemName(ClientInfo clientInfo)
    {
        var os = clientInfo.OS.Family;
        return string.IsNullOrWhiteSpace(os) ? "Unknown OS" : os;
    }
}