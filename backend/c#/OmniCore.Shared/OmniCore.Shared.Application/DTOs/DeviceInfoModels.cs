namespace OmniCore.Shared.Application.DTOs;

public record DeviceInfo(
    string DeviceId,
    string? DeviceName,
    string? OperatingSystem,
    string? Browser,
    string? IpAddress);