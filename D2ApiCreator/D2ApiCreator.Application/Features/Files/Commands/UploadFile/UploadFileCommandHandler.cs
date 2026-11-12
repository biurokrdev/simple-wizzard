using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Common.Storage;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace D2ApiCreator.Application.Features.Files.Commands.UploadFile;

/// <summary>
/// Upload szablonu do wersji projektu. Treść pliku trafia do zewnętrznego storage (GCS) pod kluczem
/// <see cref="FileStorageKeys.Template"/>, a do bazy wyłącznie metadane + <c>storage_ref</c> (ADR-0005).
/// Kolejność: najpierw storage, potem baza; przy błędzie zapisu do bazy obiekt jest usuwany (best-effort),
/// żeby nie zostawiać osieroconych blobów.
/// </summary>
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Guid>
{
    private readonly IFileRepository _fileRepository;
    private readonly IProjectVersionRepository _projectVersionRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IFileRepository fileRepository,
        IProjectVersionRepository projectVersionRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ILogger<UploadFileCommandHandler> logger)
    {
        _fileRepository = fileRepository;
        _projectVersionRepository = projectVersionRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading file {FileName} (Content-Type: {ContentType}, FileType: {FileType}) to project version {ProjectVersionId}",
            request.File.FileName, request.File.ContentType, request.FileType ?? "null", request.ProjectVersionId);

        // Verify project version exists
        var projectVersion = await _projectVersionRepository.GetByIdAsync(request.ProjectVersionId, cancellationToken);
        if (projectVersion == null)
        {
            _logger.LogError("Project version {ProjectVersionId} NOT FOUND in database", request.ProjectVersionId);
            throw new InvalidOperationException($"Project version {request.ProjectVersionId} not found");
        }

        // Read file content
        byte[] fileContent;
        using (var memoryStream = new MemoryStream())
        {
            await request.File.OpenReadStream().CopyToAsync(memoryStream, cancellationToken);
            fileContent = memoryStream.ToArray();
        }

        // Calculate SHA256 hash
        var sha256Hash = Convert.ToHexString(SHA256.HashData(fileContent)).ToLowerInvariant();

        // Upload binary to external storage (GCS) first — the DB row references the object key
        var fileId = Guid.NewGuid();
        var objectName = FileStorageKeys.Template(request.ProjectVersionId, fileId);
        var storageRef = await _fileStorage.UploadAsync(objectName, fileContent, request.File.ContentType, cancellationToken);

        var fileEntity = new FileEntity(
            id: fileId,
            projectVersionId: request.ProjectVersionId,
            filename: request.File.FileName,
            content: null,
            contentType: request.File.ContentType,
            size: fileContent.Length,
            sha256: sha256Hash,
            storageRef: storageRef
        );

        try
        {
            await _fileRepository.AddAsync(fileEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving file {FileId} metadata failed — removing orphaned object {StorageRef}", fileId, storageRef);
            await TryDeleteOrphanAsync(storageRef);
            throw;
        }

        _logger.LogInformation("Uploaded file {FileId} ({FileName}, {Size} bytes, sha256 {Sha256}) to project version {ProjectVersionId} as {StorageRef}",
            fileId, request.File.FileName, fileContent.Length, sha256Hash, request.ProjectVersionId, storageRef);

        return fileId;
    }

    private async Task TryDeleteOrphanAsync(string storageRef)
    {
        try
        {
            // Not linked to the request token on purpose: cleanup should run even when the request was cancelled.
            await _fileStorage.DeleteAsync(storageRef, CancellationToken.None);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx, "Could not remove orphaned object {StorageRef} from storage", storageRef);
        }
    }
}
