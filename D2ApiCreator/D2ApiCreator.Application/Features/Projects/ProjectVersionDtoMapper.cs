using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Application.Features.Projects;

/// <summary>Jedno mapowanie wersji na DTO (pola przeglądu BR-017 włącznie).</summary>
public static class ProjectVersionDtoMapper
{
    public static ProjectVersionDto ToDto(ProjectVersion version) => new()
    {
        Id = version.Id,
        ProjectId = version.ProjectId,
        Major = version.Major,
        Minor = version.Minor,
        VersionTag = version.VersionTag,
        IsActive = version.IsActive,
        Status = version.Status,
        CreatedBy = version.CreatedBy,
        CreatedAt = version.CreatedAt,
        StepData = version.StepData,
        Notes = version.Notes,
        SubmittedAt = version.SubmittedAt,
        ReviewedAt = version.ReviewedAt,
        ReviewedBy = version.ReviewedBy,
        ReviewComment = version.ReviewComment,
        ReviewNotes = ReadNotes(version.ReviewNotes),
        DeliveredAt = version.DeliveredAt,
        DeliveredTo = version.DeliveredTo,
        DeliveredPackageSha = version.DeliveredPackageSha
    };

    /// <summary>JSONB { krok: tekst } → słownik (puste wpisy pomijane).</summary>
    public static Dictionary<string, string>? ReadNotes(System.Text.Json.JsonDocument? notes)
    {
        if (notes is null || notes.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
        var result = new Dictionary<string, string>();
        foreach (var p in notes.RootElement.EnumerateObject())
        {
            if (p.Value.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var text = p.Value.GetString();
                if (!string.IsNullOrWhiteSpace(text)) result[p.Name] = text;
            }
        }
        return result.Count == 0 ? null : result;
    }
}
