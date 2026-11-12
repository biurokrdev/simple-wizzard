using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Packages;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Packages;

/// <summary>Buduje paczkę (ZIP) wersji pisma: manifest + wszystkie workery + plik szablonu wgrany w tej wersji.</summary>
public record BuildProjectPackageQuery(Guid ProjectId, Guid VersionId, string? RequestedBy) : IRequest<Result<ProjectPackage>>;

/// <summary>Buduje paczkę i wysyła do aplikacji docelowej; zapisuje na wersji datę, cel i skrót paczki (BR-022).</summary>
public record PublishProjectPackageCommand(Guid ProjectId, Guid VersionId, string? SentBy) : IRequest<Result<PackageDeliveryDto>>;

public class BuildProjectPackageQueryHandler : IRequestHandler<BuildProjectPackageQuery, Result<ProjectPackage>>
{
    private readonly IRepository<Project, Guid> _projects;
    private readonly IProjectVersionRepository _versions;
    private readonly IWorkerRepository _workers;
    private readonly IRepository<Tenant, Guid> _tenants;
    private readonly IFileStorageService _storage;
    private readonly ILogger<BuildProjectPackageQueryHandler> _logger;

    public BuildProjectPackageQueryHandler(
        IRepository<Project, Guid> projects,
        IProjectVersionRepository versions,
        IWorkerRepository workers,
        IRepository<Tenant, Guid> tenants,
        IFileStorageService storage,
        ILogger<BuildProjectPackageQueryHandler> logger)
    {
        _projects = projects;
        _versions = versions;
        _workers = workers;
        _tenants = tenants;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<ProjectPackage>> Handle(BuildProjectPackageQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            return Result<ProjectPackage>.Failure($"Project with ID {request.ProjectId} not found");
        }

        var version = await _versions.GetWithDetailsAsync(request.VersionId, cancellationToken);
        if (version == null || version.ProjectId != project.Id)
        {
            return Result<ProjectPackage>.Failure($"Version with ID {request.VersionId} not found in project {request.ProjectId}");
        }

        var workers = (await _workers.GetByProjectVersionIdAsync(version.Id, cancellationToken)).ToList();

        // Szablon wgrany w tej wersji: najnowszy plik wersji (ten sam wybór co na listach projektów)
        var template = version.Files.OrderByDescending(f => f.CreatedAt).FirstOrDefault();
        byte[]? templateBytes = null;
        if (template != null)
        {
            try
            {
                templateBytes = template.IsInExternalStorage
                    ? await _storage.DownloadAsync(template.StorageRef!, cancellationToken)
                    : template.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Template {File} of version {VersionId} could not be read from storage", template.Filename, version.Id);
            }
            if (templateBytes == null || templateBytes.Length == 0)
            {
                return Result<ProjectPackage>.Failure(
                    $"Template file '{template.Filename}' is not available in storage — upload the template again before packaging");
            }
        }

        var tenantName = project.TenantId.HasValue ? (await _tenants.GetByIdAsync(project.TenantId.Value, cancellationToken))?.Name : null;

        var package = ProjectPackageBuilder.Build(project, version, workers, template, templateBytes, tenantName, request.RequestedBy);
        _logger.LogInformation("Package {FileName} built for version {VersionId}: {Workers} workers, template={Template}, {Size} bytes, sha256={Sha}",
            package.FileName, version.Id, workers.Count, template?.Filename ?? "-", package.Content.Length, package.Sha256);
        return Result<ProjectPackage>.Success(package);
    }
}

public class PublishProjectPackageCommandHandler : IRequestHandler<PublishProjectPackageCommand, Result<PackageDeliveryDto>>
{
    private readonly ISender _sender;
    private readonly IPackageDeliveryService _delivery;
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PublishProjectPackageCommandHandler> _logger;

    public PublishProjectPackageCommandHandler(
        ISender sender,
        IPackageDeliveryService delivery,
        IRepository<ProjectVersion, Guid> versions,
        IUnitOfWork unitOfWork,
        ILogger<PublishProjectPackageCommandHandler> logger)
    {
        _sender = sender;
        _delivery = delivery;
        _versions = versions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PackageDeliveryDto>> Handle(PublishProjectPackageCommand request, CancellationToken cancellationToken)
    {
        if (!_delivery.IsConfigured)
        {
            return Result<PackageDeliveryDto>.Failure(
                "Target application address is not configured (TargetApplication:PackageUrl) — download the package instead");
        }

        var built = await _sender.Send(new BuildProjectPackageQuery(request.ProjectId, request.VersionId, request.SentBy), cancellationToken);
        if (!built.IsSuccess || built.Value == null)
        {
            return Result<PackageDeliveryDto>.Failure(built.Error ?? "Package could not be built");
        }
        var package = built.Value;

        var result = await _delivery.SendAsync(package, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("Package {FileName} rejected by {Target}: {Error}", package.FileName, result.Target, result.Error);
            return Result<PackageDeliveryDto>.Failure($"Target application rejected the package: {result.Error}");
        }

        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        var deliveredAt = DateTime.UtcNow;
        if (version != null)
        {
            version.MarkDelivered(deliveredAt, result.Target, package.Sha256);
            await _versions.UpdateAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Package {FileName} delivered to {Target} (sha256 {Sha})", package.FileName, result.Target, package.Sha256);
        return Result<PackageDeliveryDto>.Success(new PackageDeliveryDto
        {
            ProjectId = request.ProjectId,
            VersionId = request.VersionId,
            VersionTag = package.Manifest.Version.Tag,
            FileName = package.FileName,
            Size = package.Content.LongLength,
            Sha256 = package.Sha256,
            WorkerCount = package.Manifest.Workers.Count,
            HasTemplate = package.Manifest.Template != null,
            DeliveredAt = deliveredAt,
            DeliveredTo = result.Target,
            TargetResponse = result.ResponseSummary
        });
    }
}
