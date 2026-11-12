namespace D2ApiCreator.Application.DTOs.WorkerProfiles;

/// <summary>Profil workera (katalog typów) — odpowiedź API.</summary>
public class WorkerProfileDto
{
    public Guid Id { get; set; }
    /// <summary>Typ workera = wartość <c>workers.worker_type</c>.</summary>
    public string Key { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>Domyślna konfiguracja (JSON jako tekst); token <c>{name}</c> = nazwa elementu.</summary>
    public string DefaultConfigJson { get; set; } = "{}";
    public bool IsVisible { get; set; }
    public int SortOrder { get; set; }
    /// <summary>"Synced" | "Manual"</summary>
    public string Source { get; set; } = null!;
    public bool IsMissingInSource { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Ręczne dodanie profilu przez administratora.</summary>
public class CreateWorkerProfileDto
{
    public string Key { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string DefaultConfigJson { get; set; } = "{}";
    public bool IsVisible { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Edycja profilu (klucz jest niezmienny).</summary>
public class UpdateWorkerProfileDto
{
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string DefaultConfigJson { get; set; } = "{}";
    public bool IsVisible { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Wynik synchronizacji z katalogiem aplikacji docelowej.</summary>
public class WorkerProfileSyncResultDto
{
    public string Source { get; set; } = null!;
    public DateTime SyncedAt { get; set; }
    /// <summary>Nowe typy (dodane jako ukryte).</summary>
    public List<string> Added { get; set; } = new();
    /// <summary>Typy potwierdzone w katalogu (edycje administratora zachowane).</summary>
    public List<string> Confirmed { get; set; } = new();
    /// <summary>Typy zsynchronizowane wcześniej, których katalog już nie zwraca.</summary>
    public List<string> Missing { get; set; } = new();
    public int Total { get; set; }
}
