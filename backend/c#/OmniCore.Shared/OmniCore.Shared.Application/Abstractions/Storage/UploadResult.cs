namespace OmniCore.Shared.Application.Abstractions.Storage;

public record UploadResult(
    string ObjectKey,
    string PublicUrl,
    string FileName,
    string ContentType,
    long FileSize);