using D2ApiCreator.Application.Common.Interfaces;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Implementacja <see cref="IFileStorageService"/> na Google Cloud Storage.
/// Działa zarówno z produkcyjnym GCS, jak i z fake-gcs-server (DEV/test) — różnica tylko w <see cref="StorageClient"/>
/// zbudowanym w <c>DependencyInjection</c>. Wzór: <c>GcsDocumentStorageService</c> w D2 ViewerEditor.
/// </summary>
public class GcsFileStorageService : IFileStorageService
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GcsFileStorageService> _logger;

    public GcsFileStorageService(
        StorageClient storageClient,
        IOptions<GcsStorageOptions> options,
        ILogger<GcsFileStorageService> logger)
    {
        _storageClient = storageClient;
        _bucketName = options.Value.BucketName;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string objectName, byte[] content, string? contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectName,
            string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType,
            stream,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Uploaded {ObjectName} ({Size} bytes) to bucket {Bucket}", objectName, content.Length, _bucketName);

        return objectName;
    }

    public async Task<byte[]> DownloadAsync(string storageRef, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await _storageClient.DownloadObjectAsync(
            _bucketName,
            storageRef,
            stream,
            cancellationToken: cancellationToken);

        return stream.ToArray();
    }

    public async Task DeleteAsync(string storageRef, CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageClient.DeleteObjectAsync(_bucketName, storageRef, cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted {ObjectName} from bucket {Bucket}", storageRef, _bucketName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Object {ObjectName} not found in bucket {Bucket} during delete", storageRef, _bucketName);
        }
    }

    public async Task<bool> ExistsAsync(string storageRef, CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageClient.GetObjectAsync(_bucketName, storageRef, cancellationToken: cancellationToken);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
