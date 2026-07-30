namespace OmniCore.Shared.Application.DTOs;

public record LogNotificationDto(
    string Category,
    string Message,
    string Level,
    DateTime TimestampUtc);