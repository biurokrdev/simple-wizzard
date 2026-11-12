using D2ApiCreator.Application.Common.Interfaces;

namespace D2ServicesCreator.Api.Services;

/// <summary>
/// Template binaries are out of scope for the Services API — it never uploads, reads or deletes
/// files. The shared Application layer still registers <c>UploadFileCommandHandler</c>, which
/// takes <see cref="IFileStorageService"/> in its constructor, so DI validation (ValidateOnBuild
/// in Development) needs *some* implementation. This one fails loudly instead of silently talking
/// to a storage this host was never configured for. When <c>GoogleCloudStorage:BucketName</c> is set,
/// Infrastructure registers the real GCS service first and this stub is not used (TryAdd).
/// </summary>
public sealed class UnavailableFileStorageService : IFileStorageService
{
    private const string Message =
        "File storage is not available in the D2ServicesCreator API. Template uploads go through D2ApiCreator (/api/v1/Files).";

    public Task<string> UploadAsync(string objectName, byte[] content, string? contentType, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(Message);

    public Task<byte[]> DownloadAsync(string storageRef, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(Message);

    public Task DeleteAsync(string storageRef, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(Message);

    public Task<bool> ExistsAsync(string storageRef, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(Message);
}
