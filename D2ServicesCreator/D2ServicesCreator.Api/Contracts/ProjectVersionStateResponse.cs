using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Enums;

namespace D2ServicesCreator.Api.Contracts;

/// <summary>
/// State of a single project version as seen by external systems. Deliberately narrow: no
/// <c>stepData</c> (the wizard's JSONB blob), no notes, no author — only what an integrating
/// application needs to decide whether a letter template is switched on.
/// </summary>
/// <param name="ProjectId">Project (letter) identifier.</param>
/// <param name="VersionId">Version identifier used in activate/deactivate routes.</param>
/// <param name="VersionTag">Human-readable version, e.g. <c>1.0</c>.</param>
/// <param name="IsActive">Whether this version is the one currently in force.</param>
/// <param name="Status">Lifecycle status (serialised as the enum name).</param>
public sealed record ProjectVersionStateResponse(
    Guid ProjectId,
    Guid VersionId,
    string VersionTag,
    bool IsActive,
    ProjectStatus Status)
{
    public static ProjectVersionStateResponse From(ProjectVersionDto version) =>
        new(version.ProjectId, version.Id, version.VersionTag, version.IsActive, version.Status);
}
