namespace OmniCore.Shared.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<UploadResult> UploadAsync(
        IFileUpload file,
        string? folder = null,
        CancellationToken cancellationToken = default);

    Task<UploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    Task<UploadResult> UploadAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKeyOrUrl, 
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(
        string objectKeyOrUrl, 
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string objectKey);
}