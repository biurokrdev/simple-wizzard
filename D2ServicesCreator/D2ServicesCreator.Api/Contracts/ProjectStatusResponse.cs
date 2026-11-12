using D2ApiCreator.Application.DTOs.Projects;

namespace D2ServicesCreator.Api.Contracts;

/// <summary>
/// Narrowed read model of a project (letter template) for external systems. The full
/// <c>ProjectDto</c> / <c>ProjectVersionDto</c> contract of D2ApiCreator (<c>/api/v1</c>) is not
/// exposed here — metadata, retention, tenant and the wizard's <c>stepData</c> stay internal.
/// </summary>
/// <param name="ProjectId">Project identifier (GUID used in routes).</param>
/// <param name="Source">Business identifier of the project, e.g. <c>PRJ-20260828-F1111183</c>.</param>
/// <param name="Name">Project name.</param>
/// <param name="IsActive">True when the project has an active version.</param>
/// <param name="ActiveVersionId">Identifier of the active version, or null.</param>
/// <param name="ActiveVersionTag">Tag of the active version (e.g. <c>1.2</c>), or null.</param>
/// <param name="Versions">All versions of the project, newest first.</param>
public sealed record ProjectStatusResponse(
    Guid ProjectId,
    string Source,
    string Name,
    bool IsActive,
    Guid? ActiveVersionId,
    string? ActiveVersionTag,
    IReadOnlyList<ProjectVersionStateResponse> Versions)
{
    public static ProjectStatusResponse From(ProjectDto project, IEnumerable<ProjectVersionDto> versions)
    {
        var states = versions.Select(ProjectVersionStateResponse.From).ToList();

        // The project's pointer (projects.current_active_version_id) is the source of truth for
        // "which version"; the version's own flag confirms it is really switched on.
        var active = project.CurrentActiveVersionId is { } activeId
            ? states.FirstOrDefault(v => v.VersionId == activeId)
            : null;

        return new ProjectStatusResponse(
            project.Id,
            project.Source,
            project.Name,
            active is { IsActive: true },
            project.CurrentActiveVersionId,
            active?.VersionTag,
            states);
    }
}
