using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Projects.Queries.GetProjects;

/// <summary>
/// Handler for getting projects with pagination
/// </summary>
/// <remarks>
/// Template shown on the list: the active version's template when the project has one,
/// otherwise the newest (draft) version's template — see <see cref="ResolveTemplateVersion"/>.
/// </remarks>
public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<PagedResult<ProjectDto>>>
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IProjectVersionRepository _versionRepository;
    private readonly IRepository<TenantMember, Guid> _tenantMemberRepository;
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly ILogger<GetProjectsQueryHandler> _logger;

    public GetProjectsQueryHandler(
        IRepository<Project, Guid> projectRepository,
        IProjectVersionRepository versionRepository,
        IRepository<TenantMember, Guid> tenantMemberRepository,
        IRepository<Tenant, Guid> tenantRepository,
        ILogger<GetProjectsQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _versionRepository = versionRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting projects. TenantId: {TenantId}, Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
                request.TenantId, request.PageNumber, request.PageSize, request.SearchTerm);

            var projects = await _projectRepository.GetAllAsync(cancellationToken);

            // Widoczność dla projektanta (klucz korporacyjny podany): projekty zespołów, których jest aktywnym
            // (niezablokowanym) członkiem i które same nie są zablokowane, oraz własne projekty prywatne (bez zespołu).
            // Bez klucza (panel administracyjny) — wszystkie projekty.
            if (!string.IsNullOrEmpty(request.CurrentUserCorporateKey))
            {
                var userMemberships = await _tenantMemberRepository.FindAsync(
                    m => m.CorporateKey == request.CurrentUserCorporateKey && !m.IsBlocked,
                    cancellationToken);
                var memberTenantIds = userMemberships.Select(m => m.TenantId).Distinct().ToList();
                var activeTenantIds = memberTenantIds.Count == 0
                    ? new HashSet<Guid>()
                    : (await _tenantRepository.FindAsync(t => memberTenantIds.Contains(t.Id) && t.IsActive, cancellationToken))
                        .Select(t => t.Id).ToHashSet();

                projects = projects.Where(p => IsVisibleTo(p, request.CurrentUserCorporateKey, activeTenantIds)).ToList();
            }

            // Apply filters
            if (request.TenantId.HasValue)
            {
                projects = projects.Where(p => p.TenantId == request.TenantId.Value).ToList();
            }

            if (!string.IsNullOrEmpty(request.Source))
            {
                projects = projects.Where(p => p.Source.Contains(request.Source, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                projects = projects.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Source.ToLower().Contains(searchLower)).ToList();
            }

            if (request.DateFrom.HasValue)
            {
                projects = projects.Where(p => p.CreatedAt >= request.DateFrom.Value).ToList();
            }

            if (request.DateTo.HasValue)
            {
                var dateTo = request.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                projects = projects.Where(p => p.CreatedAt <= dateTo).ToList();
            }

            var projectCount = projects.Count();

            // Order by created date descending and apply pagination
            var pagedItems = projects
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling(projectCount / (double)request.PageSize);

            // One query: every version (with its template filename) of the projects on this page.
            var versionsByProject = pagedItems.Count == 0
                ? new Dictionary<Guid, List<ProjectVersionTemplateInfo>>()
                : (await _versionRepository.GetTemplateInfoByProjectIdsAsync(
                        pagedItems.Select(p => p.Id).Distinct().ToList(), cancellationToken))
                    .GroupBy(v => v.ProjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());

            // Nazwy zespołów dla projektów na stronie (jedno zapytanie)
            var tenantIdsOnPage = pagedItems.Where(p => p.TenantId.HasValue).Select(p => p.TenantId!.Value).Distinct().ToList();
            var tenantNames = tenantIdsOnPage.Count == 0
                ? new Dictionary<Guid, string>()
                : (await _tenantRepository.FindAsync(t => tenantIdsOnPage.Contains(t.Id), cancellationToken)).ToDictionary(t => t.Id, t => t.Name);

            // Map to DTOs
            var projectDtos = pagedItems.Select(p =>
            {
                versionsByProject.TryGetValue(p.Id, out var versions);
                var templateVersion = ResolveTemplateVersion(p, versions);
                var activeVersion = templateVersion is { IsActive: true } ? templateVersion : null;

                return new ProjectDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    TenantName = p.TenantId.HasValue && tenantNames.TryGetValue(p.TenantId.Value, out var tenantName) ? tenantName : null,
                    OwnerCorporateKey = p.Metadata.OwnerCorporateKey,
                    Source = p.Source,
                    Name = p.Name,
                    DefaultTemplateName = p.DefaultTemplateName,
                    CreatedBy = p.CreatedBy,
                    CreatedAt = p.CreatedAt,
                    RetentionDays = p.RetentionDays,
                    Description = p.Description,
                    CurrentActiveVersionId = p.CurrentActiveVersionId,
                    ActiveVersionTag = activeVersion?.VersionTag,
                    ActiveVersionTemplateName = activeVersion?.TemplateFilename,
                    TemplateName = templateVersion?.TemplateFilename ?? p.DefaultTemplateName,
                    TemplateVersionTag = templateVersion?.VersionTag,
                    TemplateVersionIsActive = templateVersion?.IsActive ?? false
                };
            }).ToList();

            var result = new PagedResult<ProjectDto>
            {
                Items = projectDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = projectCount,
                TotalPages = totalPages
            };

            _logger.LogInformation("Found {Count} projects (page {Page} of {TotalPages})",
                projectDtos.Count, request.PageNumber, totalPages);

            return Result<PagedResult<ProjectDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects");
            return Result<PagedResult<ProjectDto>>.Failure($"Error getting projects: {ex.Message}");
        }
    }

    /// <summary>
    /// Visibility of a project for a designer: a team project is visible to active members of an active
    /// team; a private project (no team) only to its owner (metadata.OwnerCorporateKey). Legacy private
    /// projects without an owner stay visible only in the admin module (no corporate key).
    /// </summary>
    public static bool IsVisibleTo(Project project, string corporateKey, ISet<Guid> activeMemberTenantIds)
    {
        if (project.TenantId.HasValue)
        {
            return activeMemberTenantIds.Contains(project.TenantId.Value);
        }
        return string.Equals(project.Metadata.OwnerCorporateKey, corporateKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Picks the version whose template the list should show: the active one (by the project's
    /// pointer, falling back to the <c>IsActive</c> flag), otherwise the newest version by number
    /// and creation date. Returns <c>null</c> when the project has no versions yet.
    /// </summary>
    public static ProjectVersionTemplateInfo? ResolveTemplateVersion(Project project, IReadOnlyList<ProjectVersionTemplateInfo>? versions)
    {
        if (versions is null || versions.Count == 0)
        {
            return null;
        }

        var active = project.CurrentActiveVersionId.HasValue
            ? versions.FirstOrDefault(v => v.VersionId == project.CurrentActiveVersionId.Value)
            : null;
        active ??= versions.FirstOrDefault(v => v.IsActive);

        return active ?? versions
            .OrderByDescending(v => v.Major)
            .ThenByDescending(v => v.Minor)
            .ThenByDescending(v => v.CreatedAt)
            .First();
    }
}
