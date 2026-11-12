using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Application.Features.WorkerProfiles;

internal static class WorkerProfileMapper
{
    public static WorkerProfileDto ToDto(WorkerProfile profile) => new()
    {
        Id = profile.Id,
        Key = profile.Key,
        DisplayName = profile.DisplayName,
        Description = profile.Description,
        DefaultConfigJson = profile.DefaultConfigJson.RootElement.GetRawText(),
        IsVisible = profile.IsVisible,
        SortOrder = profile.SortOrder,
        Source = profile.Source.ToString(),
        IsMissingInSource = profile.IsMissingInSource,
        LastSyncedAt = profile.LastSyncedAt,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
