using System.Text.Json;
using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Commands.CloneProjectVersion;

/// <summary>
/// Copies a project version into a new draft: next minor number (BR-003: 1.0 → 1.1), the same
/// <c>step_data</c>, the current template file (newest file of the source) and all workers.
/// </summary>
/// <remarks>
/// The copied file row points at the same storage object (<c>storage_ref</c>) — binaries are not
/// duplicated in GCS. Deleting a version does not delete objects today (R-22), so sharing is safe.
/// </remarks>
public class CloneProjectVersionCommandHandler : IRequestHandler<CloneProjectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<ProjectVersion, Guid> _versionRepository;
    private readonly IFileRepository _fileRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloneProjectVersionCommandHandler> _logger;

    public CloneProjectVersionCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<ProjectVersion, Guid> versionRepository,
        IFileRepository fileRepository,
        IWorkerRepository workerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CloneProjectVersionCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _fileRepository = fileRepository;
        _workerRepository = workerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(CloneProjectVersionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                return Result<ProjectVersionDto>.Failure($"Project with ID {request.ProjectId} not found");
            }

            var source = await _versionRepository.GetByIdAsync(request.SourceVersionId, cancellationToken);
            if (source == null)
            {
                return Result<ProjectVersionDto>.Failure($"Version with ID {request.SourceVersionId} not found");
            }

            if (source.ProjectId != request.ProjectId)
            {
                return Result<ProjectVersionDto>.Failure("Version does not belong to the specified project");
            }

            var existingVersions = (await _versionRepository.FindAsync(
                v => v.ProjectId == request.ProjectId, cancellationToken)).ToList();
            var (major, minor) = NextVersionNumber(existingVersions);

            var clone = new ProjectVersion(Guid.NewGuid(), request.ProjectId, major, minor, request.CreatedBy);
            if (source.StepData != null)
            {
                clone.UpdateStepData(CloneJson(source.StepData));
            }
            clone.UpdateNotes($"Utworzona na podstawie wersji {source.VersionTag}");
            await _versionRepository.AddAsync(clone, cancellationToken);

            // Current template = newest file of the source version (re-uploads append rows).
            var sourceFiles = await _fileRepository.GetByProjectVersionIdAsync(source.Id, cancellationToken);
            var currentFile = sourceFiles.OrderByDescending(f => f.CreatedAt).FirstOrDefault();
            if (currentFile != null)
            {
                var fileCopy = new FileEntity(
                    Guid.NewGuid(),
                    clone.Id,
                    currentFile.Filename,
                    currentFile.Content,
                    currentFile.ContentType,
                    currentFile.Size,
                    currentFile.Sha256,
                    currentFile.StorageRef);
                await _fileRepository.AddAsync(fileCopy, cancellationToken);
            }

            var sourceWorkers = await _workerRepository.GetByProjectVersionIdAsync(source.Id, cancellationToken);
            var copiedWorkers = 0;
            foreach (var worker in sourceWorkers)
            {
                await _workerRepository.AddAsync(
                    new Worker(Guid.NewGuid(), clone.Id, worker.Name, worker.WorkerType, CloneJson(worker.ConfigJson)),
                    cancellationToken);
                copiedWorkers++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cloned version {SourceTag} of project {ProjectId} into draft {NewTag} ({Workers} workers, file: {File})",
                source.VersionTag, request.ProjectId, clone.VersionTag, copiedWorkers, currentFile?.Filename ?? "-");

            return Result<ProjectVersionDto>.Success(new ProjectVersionDto
            {
                Id = clone.Id,
                ProjectId = clone.ProjectId,
                Major = clone.Major,
                Minor = clone.Minor,
                VersionTag = clone.VersionTag,
                IsActive = clone.IsActive,
                Status = clone.Status,
                CreatedBy = clone.CreatedBy,
                CreatedAt = clone.CreatedAt,
                StepData = clone.StepData,
                Notes = clone.Notes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning version {VersionId} of project {ProjectId}", request.SourceVersionId, request.ProjectId);
            return Result<ProjectVersionDto>.Failure($"Failed to clone version: {ex.Message}");
        }
    }

    /// <summary>Next number after the highest existing one: same major, minor + 1 (BR-003). No versions → 1.0.</summary>
    public static (int Major, int Minor) NextVersionNumber(IReadOnlyCollection<ProjectVersion> existing)
    {
        if (existing.Count == 0)
        {
            return (1, 0);
        }
        var max = existing.OrderByDescending(v => v.Major).ThenByDescending(v => v.Minor).First();
        return (max.Major, max.Minor + 1);
    }

    private static JsonDocument CloneJson(JsonDocument source) => JsonDocument.Parse(source.RootElement.GetRawText());
}
