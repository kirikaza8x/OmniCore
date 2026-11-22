namespace OmniCore.Shared.Infrastructure.Services.Storage;

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Application.Abstractions.Storage;
using OmniCore.Shared.Infrastructure.Configs.Storage;

/// <summary>
/// Storage service implementation for MinIO and Amazon S3 object storage compatibility.
/// </summary>
public sealed class MinioStorageService : IStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageConfig _config;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketChecked;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinioStorageService"/> class.
    /// </summary>
    public MinioStorageService(
        IOptions<StorageConfig> config,
        ILogger<MinioStorageService> logger)
    {
        _config = config.Value;
        _logger = logger;

        var endpoint = _config.Endpoint;
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = _config.UseSSL ? $"https://{endpoint}" : $"http://{endpoint}";
        }

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true, // Required for MinIO
            UseHttp = !_config.UseSSL
        };

        _s3Client = new AmazonS3Client(
            _config.AccessKey,
            _config.SecretKey,
            s3Config);
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(
        IFileUpload file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var stream = file.OpenReadStream();
        return await UploadAsync(stream, file.FileName, file.ContentType, folder, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await EnsureBucketExistsAsync(cancellationToken);

        var objectKey = GenerateObjectKey(fileName, folder);
        long fileSize = fileStream.CanSeek ? fileStream.Length : 0;

        try
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = objectKey,
                BucketName = _config.BucketName,
                ContentType = contentType
            };

            using var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            var publicUrl = GetPublicUrl(objectKey);

            _logger.LogInformation("File uploaded successfully: {ObjectKey}", objectKey);

            return new UploadResult(
                ObjectKey: objectKey,
                PublicUrl: publicUrl,
                FileName: fileName,
                ContentType: contentType,
                FileSize: fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        using var stream = new MemoryStream(fileBytes);
        return await UploadAsync(stream, fileName, contentType, folder, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKeyOrUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKeyOrUrl);

        var objectKey = ExtractObjectKeyFromUrl(objectKeyOrUrl);

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _config.BucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("File deleted successfully: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> DownloadAsync(string objectKeyOrUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKeyOrUrl);

        var objectKey = ExtractObjectKeyFromUrl(objectKeyOrUrl);

        try
        {
            var response = await _s3Client.GetObjectAsync(
                _config.BucketName,
                objectKey,
                cancellationToken);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found: {ObjectKey}", objectKey);
            return null;
        }
    }

    /// <inheritdoc />
    public string GetPublicUrl(string objectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        if (!string.IsNullOrWhiteSpace(_config.PublicUrl))
        {
            return $"{_config.PublicUrl.TrimEnd('/')}/{objectKey}";
        }

        var endpoint = _config.Endpoint.TrimEnd('/');
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = _config.UseSSL ? $"https://{endpoint}" : $"http://{endpoint}";
        }

        return $"{endpoint}/{_config.BucketName}/{objectKey}";
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        if (_bucketChecked) return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketChecked) return;

            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(
                _s3Client,
                _config.BucketName);

            if (!bucketExists)
            {
                _logger.LogInformation("Creating bucket: {BucketName}", _config.BucketName);

                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _config.BucketName,
                    UseClientRegion = true
                }, cancellationToken);

                var policy = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Effect": "Allow",
                            "Principal": "*",
                            "Action": ["s3:GetObject"],
                            "Resource": ["arn:aws:s3:::{{_config.BucketName}}/*"]
                        }
                    ]
                }
                """;

                await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                {
                    BucketName = _config.BucketName,
                    Policy = policy
                }, cancellationToken);

                _logger.LogInformation("Bucket created successfully: {BucketName}", _config.BucketName);
            }

            _bucketChecked = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists: {BucketName}", _config.BucketName);
            throw;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    // The key is built from a random id plus a cleaned-up copy of the client's file name; the folder is cleaned
    // segment by segment, so neither can climb out of the intended prefix or smuggle odd characters into the URL.
    internal static string GenerateObjectKey(string fileName, string? folder)
    {
        var uniqueFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        var cleanFolder = SanitizeFolder(folder);

        return cleanFolder.Length == 0 ? uniqueFileName : $"{cleanFolder}/{uniqueFileName}";
    }

    internal static string SanitizeFileName(string fileName)
    {
        // keep only the last path part, letters, digits, dot, dash, underscore; no leading dots; at most 100 chars
        var name = fileName.Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];

        var cleaned = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray())
            .TrimStart('.');
        while (cleaned.Contains("..", StringComparison.Ordinal)) cleaned = cleaned.Replace("..", ".");

        if (cleaned.Length > 100) cleaned = cleaned[^100..];
        return cleaned.Length == 0 ? "file" : cleaned;
    }

    internal static string SanitizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return string.Empty;

        var segments = folder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(seg => new string(seg.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray()))
            .Where(seg => seg.Length > 0);

        return string.Join('/', segments);
    }

    private string ExtractObjectKeyFromUrl(string fileUrl)
    {
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            var bucketPrefix = $"{_config.BucketName}/";

            if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
            {
                path = path[bucketPrefix.Length..];
            }
            return path;
        }
        return fileUrl;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _s3Client.Dispose();
        _bucketLock.Dispose();
    }
}