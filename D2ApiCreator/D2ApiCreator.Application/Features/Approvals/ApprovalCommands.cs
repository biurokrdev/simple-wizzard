using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Approvals;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Approvals;

/// <summary>Projektant zgłasza wersję do publikacji: Draft | RequiresChanges → PendingApproval (BR-017).</summary>
public record SubmitVersionForReviewCommand(Guid VersionId, string? SubmittedBy) : IRequest<Result<ProjectVersionDto>>;

/// <summary>Administrator akceptuje: PendingApproval → Approved (aktywacja pozostaje osobną decyzją).</summary>
public record ApproveVersionCommand(Guid VersionId, string? Reviewer, string? Comment) : IRequest<Result<ProjectVersionDto>>;

/// <summary>Administrator odrzuca: PendingApproval → RequiresChanges („Wymaga poprawy”) z komentarzem.</summary>
public record RejectVersionCommand(Guid VersionId, string? Reviewer, string? Comment) : IRequest<Result<ProjectVersionDto>>;

/// <summary>Recenzent zapisuje uwagi do kroków (tylko dla wersji w PendingApproval); puste wpisy są pomijane.</summary>
public record SaveReviewNotesCommand(Guid VersionId, IReadOnlyDictionary<string, string> Notes) : IRequest<Result<ProjectVersionDto>>;

/// <summary>Lista wersji czekających na decyzję (wszystkie projekty).</summary>
public record GetPendingVersionsQuery : IRequest<Result<List<PendingVersionDto>>>;

public class SubmitVersionForReviewCommandHandler : IRequestHandler<SubmitVersionForReviewCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitVersionForReviewCommandHandler(IRepository<ProjectVersion, Guid> versions, IUnitOfWork unitOfWork)
    {
        _versions = versions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectVersionDto>> Handle(SubmitVersionForReviewCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version == null)
        {
            return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
        }
        if (version.Status != ProjectStatus.Draft && version.Status != ProjectStatus.RequiresChanges)
        {
            return Result<ProjectVersionDto>.Failure($"Version {version.VersionTag} cannot be submitted in status {version.Status}");
        }

        version.SubmitForReview();
        await _versions.UpdateAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProjectVersionDto>.Success(ProjectVersionDtoMapper.ToDto(version));
    }
}

public class ApproveVersionCommandHandler : IRequestHandler<ApproveVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveVersionCommandHandler> _logger;

    public ApproveVersionCommandHandler(IRepository<ProjectVersion, Guid> versions, IUnitOfWork unitOfWork, ILogger<ApproveVersionCommandHandler> logger)
    {
        _versions = versions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(ApproveVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version == null)
        {
            return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
        }
        if (version.Status != ProjectStatus.PendingApproval)
        {
            return Result<ProjectVersionDto>.Failure($"Version {version.VersionTag} is not pending approval (status {version.Status})");
        }

        version.ApproveReview(request.Reviewer, request.Comment);
        await _versions.UpdateAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Version {VersionId} approved by {Reviewer}", version.Id, request.Reviewer ?? "-");
        return Result<ProjectVersionDto>.Success(ProjectVersionDtoMapper.ToDto(version));
    }
}

public class RejectVersionCommandHandler : IRequestHandler<RejectVersionCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectVersionCommandHandler> _logger;

    public RejectVersionCommandHandler(IRepository<ProjectVersion, Guid> versions, IUnitOfWork unitOfWork, ILogger<RejectVersionCommandHandler> logger)
    {
        _versions = versions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectVersionDto>> Handle(RejectVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version == null)
        {
            return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
        }
        if (version.Status != ProjectStatus.PendingApproval)
        {
            return Result<ProjectVersionDto>.Failure($"Version {version.VersionTag} is not pending approval (status {version.Status})");
        }

        // Komentarz ogólny jest wymagany, chyba że recenzent zostawił uwagi przy krokach (wtedy to one mówią, co poprawić).
        var comment = request.Comment?.Trim();
        if (string.IsNullOrWhiteSpace(comment))
        {
            if (!version.HasReviewNotes())
            {
                return Result<ProjectVersionDto>.Failure("Comment is required when rejecting a version");
            }
            comment = "Zobacz uwagi do kroków";
        }

        version.RejectReview(request.Reviewer, comment);
        await _versions.UpdateAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Version {VersionId} sent back for changes by {Reviewer}", version.Id, request.Reviewer ?? "-");
        return Result<ProjectVersionDto>.Success(ProjectVersionDtoMapper.ToDto(version));
    }
}

public class SaveReviewNotesCommandHandler : IRequestHandler<SaveReviewNotesCommand, Result<ProjectVersionDto>>
{
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IUnitOfWork _unitOfWork;

    public SaveReviewNotesCommandHandler(IRepository<ProjectVersion, Guid> versions, IUnitOfWork unitOfWork)
    {
        _versions = versions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectVersionDto>> Handle(SaveReviewNotesCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version == null)
        {
            return Result<ProjectVersionDto>.Failure($"Version with ID {request.VersionId} not found");
        }
        if (version.Status != ProjectStatus.PendingApproval)
        {
            return Result<ProjectVersionDto>.Failure($"Version {version.VersionTag} is not pending approval (status {version.Status})");
        }

        var clean = request.Notes
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key.Trim(), kv => kv.Value.Trim());
        version.SetReviewNotes(clean.Count == 0 ? null : System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(clean)));
        await _versions.UpdateAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProjectVersionDto>.Success(ProjectVersionDtoMapper.ToDto(version));
    }
}

public class GetPendingVersionsQueryHandler : IRequestHandler<GetPendingVersionsQuery, Result<List<PendingVersionDto>>>
{
    private readonly IRepository<ProjectVersion, Guid> _versions;
    private readonly IRepository<Project, Guid> _projects;
    private readonly IProjectVersionRepository _versionRepository;

    public GetPendingVersionsQueryHandler(IRepository<ProjectVersion, Guid> versions, IRepository<Project, Guid> projects, IProjectVersionRepository versionRepository)
    {
        _versions = versions;
        _projects = projects;
        _versionRepository = versionRepository;
    }

    public async Task<Result<List<PendingVersionDto>>> Handle(GetPendingVersionsQuery request, CancellationToken cancellationToken)
    {
        var pending = (await _versions.FindAsync(v => v.Status == ProjectStatus.PendingApproval, cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return Result<List<PendingVersionDto>>.Success(new List<PendingVersionDto>());
        }

        var projectIds = pending.Select(v => v.ProjectId).Distinct().ToList();
        var projects = (await _projects.FindAsync(p => projectIds.Contains(p.Id), cancellationToken)).ToDictionary(p => p.Id);
        var templates = (await _versionRepository.GetTemplateInfoByProjectIdsAsync(projectIds, cancellationToken))
            .ToDictionary(t => t.VersionId, t => t.TemplateFilename);

        var dtos = pending
            .OrderBy(v => v.SubmittedAt ?? v.CreatedAt)
            .Select(v =>
            {
                projects.TryGetValue(v.ProjectId, out var project);
                return new PendingVersionDto
                {
                    VersionId = v.Id,
                    ProjectId = v.ProjectId,
                    ProjectName = project?.Name ?? "?",
                    Source = project?.Source ?? "?",
                    TenantId = project?.TenantId,
                    VersionTag = v.VersionTag,
                    SubmittedAt = v.SubmittedAt,
                    CreatedBy = v.CreatedBy,
                    TemplateName = templates.TryGetValue(v.Id, out var file) ? file : null
                };
            })
            .ToList();

        return Result<List<PendingVersionDto>>.Success(dtos);
    }
}
