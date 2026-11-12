namespace D2ApiCreator.Domain.Repositories;

/// <summary>
/// Lightweight projection of a project version used by project lists: which version it is,
/// whether it is the active one and the filename of its template (first uploaded file).
/// </summary>
/// <remarks>
/// Lists show the template of the active version when there is one, otherwise the template of
/// the newest draft version — so an editor working on several letters in parallel can tell them
/// apart before anything is activated.
/// </remarks>
public sealed record ProjectVersionTemplateInfo(
    Guid VersionId,
    Guid ProjectId,
    string VersionTag,
    int Major,
    int Minor,
    bool IsActive,
    DateTime CreatedAt,
    string? TemplateFilename);
