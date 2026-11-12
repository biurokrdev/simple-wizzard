using System.Text.Json;
using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.WorkerProfiles.Commands;

/// <summary>
/// Synchronizacja profili z katalogiem typów workerów aplikacji docelowej (BR-016):
/// nowe typy → profile <b>ukryte</b> (administrator decyduje, co pokazać użytkownikom);
/// istniejące → potwierdzone, bez nadpisywania edycji administratora;
/// zsynchronizowane wcześniej, a nieobecne w katalogu → oznaczone jako brakujące (nie usuwane).
/// </summary>
public record SyncWorkerProfilesCommand : IRequest<Result<WorkerProfileSyncResultDto>>;

public class SyncWorkerProfilesCommandHandler : IRequestHandler<SyncWorkerProfilesCommand, Result<WorkerProfileSyncResultDto>>
{
    private readonly IWorkerProfileRepository _repository;
    private readonly IWorkerCatalogSource _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncWorkerProfilesCommandHandler> _logger;

    public SyncWorkerProfilesCommandHandler(
        IWorkerProfileRepository repository,
        IWorkerCatalogSource catalog,
        IUnitOfWork unitOfWork,
        ILogger<SyncWorkerProfilesCommandHandler> logger)
    {
        _repository = repository;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<WorkerProfileSyncResultDto>> Handle(SyncWorkerProfilesCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkerCatalogItem> catalog;
        try
        {
            catalog = await _catalog.GetWorkerTypesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker catalog source {Source} failed", _catalog.Name);
            return Result<WorkerProfileSyncResultDto>.Failure($"Worker catalog '{_catalog.Name}' is unavailable: {ex.Message}");
        }

        var now = DateTime.UtcNow;
        var existing = (await _repository.GetAllOrderedAsync(cancellationToken)).ToDictionary(p => p.Key, StringComparer.Ordinal);
        var result = new WorkerProfileSyncResultDto { Source = _catalog.Name, SyncedAt = now };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var nextSortOrder = existing.Values.Select(p => p.SortOrder).DefaultIfEmpty(0).Max() + 1;

        foreach (var item in catalog)
        {
            var key = item.Key.Trim();
            if (key.Length == 0 || !seen.Add(key))
            {
                continue;
            }

            if (existing.TryGetValue(key, out var profile))
            {
                profile.MarkPresentInSource(now);
                await _repository.UpdateAsync(profile, cancellationToken);
                result.Confirmed.Add(key);
                continue;
            }

            var config = ParseOrEmpty(item.DefaultConfigJson, key);
            var created = new WorkerProfile(
                Guid.NewGuid(), key,
                string.IsNullOrWhiteSpace(item.DisplayName) ? key : item.DisplayName.Trim(),
                item.Description?.Trim(), config,
                isVisible: false, sortOrder: nextSortOrder++, WorkerProfileSource.Synced);
            created.MarkPresentInSource(now);
            await _repository.AddAsync(created, cancellationToken);
            result.Added.Add(key);
        }

        foreach (var profile in existing.Values.Where(p => p.Source == WorkerProfileSource.Synced && !seen.Contains(p.Key)))
        {
            profile.MarkMissingInSource(now);
            await _repository.UpdateAsync(profile, cancellationToken);
            result.Missing.Add(profile.Key);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Total = existing.Count + result.Added.Count;

        _logger.LogInformation("Worker profiles synced from {Source}: {Added} added, {Confirmed} confirmed, {Missing} missing",
            _catalog.Name, result.Added.Count, result.Confirmed.Count, result.Missing.Count);
        return Result<WorkerProfileSyncResultDto>.Success(result);
    }

    private JsonDocument ParseOrEmpty(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonDocument.Parse("{}");
        }
        try
        {
            var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }
            document.Dispose();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Worker catalog item {Key} has invalid default config: {Message}", key, ex.Message);
        }
        return JsonDocument.Parse("{}");
    }
}
