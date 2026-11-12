namespace D2ApiCreator.Domain.Entities;

using System.Text.Json;
using Common.BaseTypes;
using D2ApiCreator.Domain.Enums;

/// <summary>
/// Profil workera — pozycja z katalogu typów workerów aplikacji docelowej (albo dodana ręcznie przez
/// administratora) wraz z opisem, domyślną konfiguracją JSON i decyzją, czy typ ma być widoczny
/// dla użytkowników kreatora (BR-016).
/// </summary>
/// <remarks>
/// <see cref="Key"/> to nazwa typu zapisywana w <c>workers.worker_type</c> (np. „Podstaw wartość”, „Stan”).
/// Synchronizacja z katalogiem aplikacji docelowej dokłada nowe typy jako <b>ukryte</b> — to administrator
/// decyduje, które pojawią się w kreatorze. Edycje administratora (nazwa, opis, konfiguracja, widoczność)
/// nie są nadpisywane przez kolejne synchronizacje; typy nieobecne w katalogu są tylko oznaczane.
/// </remarks>
public class WorkerProfile : AggregateRoot<Guid>
{
    protected WorkerProfile() { }

    public WorkerProfile(
        Guid id,
        string key,
        string displayName,
        string? description,
        JsonDocument defaultConfigJson,
        bool isVisible,
        int sortOrder,
        WorkerProfileSource source)
        : base(id)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
        DefaultConfigJson = defaultConfigJson;
        IsVisible = isVisible;
        SortOrder = sortOrder;
        Source = source;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Typ workera (wartość <c>workers.worker_type</c>); unikalny.</summary>
    public string Key { get; private set; } = null!;

    /// <summary>Nazwa pokazywana użytkownikom kreatora.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Opis działania workera (dla użytkownika i administratora).</summary>
    public string? Description { get; private set; }

    /// <summary>Domyślna konfiguracja podpowiadana w dialogu mapowania; token <c>{name}</c> = nazwa elementu.</summary>
    public JsonDocument DefaultConfigJson { get; private set; } = null!;

    /// <summary>Czy typ jest do wyboru w kreatorze.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Kolejność na liście typów.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Skąd pochodzi profil: z synchronizacji katalogu czy dodany ręcznie.</summary>
    public WorkerProfileSource Source { get; private set; }

    /// <summary>Ostatnia synchronizacja nie zwróciła tego typu (profil zsynchronizowany).</summary>
    public bool IsMissingInSource { get; private set; }

    public DateTime? LastSyncedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Edycja administratora.</summary>
    public void Update(string displayName, string? description, JsonDocument defaultConfigJson, bool isVisible, int sortOrder)
    {
        DisplayName = displayName;
        Description = description;
        DefaultConfigJson = defaultConfigJson;
        IsVisible = isVisible;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Potwierdzenie z synchronizacji: typ nadal istnieje w katalogu.</summary>
    public void MarkPresentInSource(DateTime syncedAt)
    {
        IsMissingInSource = false;
        LastSyncedAt = syncedAt;
        if (Source == WorkerProfileSource.Manual)
        {
            // Ręcznie dodany typ okazał się istnieć w katalogu — od teraz jest śledzony przez synchronizację.
            Source = WorkerProfileSource.Synced;
        }
    }

    /// <summary>Synchronizacja nie zwróciła tego typu.</summary>
    public void MarkMissingInSource(DateTime syncedAt)
    {
        IsMissingInSource = true;
        LastSyncedAt = syncedAt;
    }
}
