namespace D2ApiCreator.Application.DTOs.Packages;

/// <summary>
/// Paczka wdrożeniowa wersji pisma: ZIP z <c>manifest.json</c>, <c>workers.json</c> (wszystkie definicje workerów)
/// i plikiem szablonu wgranym w tej wersji (<c>template/&lt;nazwa&gt;</c>). Format: <see cref="PackageManifestDto.Format"/>.
/// </summary>
public class ProjectPackage
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    /// <summary>SHA-256 całego ZIP-a (hex, małe litery) — do weryfikacji po stronie aplikacji docelowej.</summary>
    public string Sha256 { get; set; } = string.Empty;
    public PackageManifestDto Manifest { get; set; } = new();
    public const string ContentType = "application/zip";
}

/// <summary>Treść <c>manifest.json</c> w paczce.</summary>
public class PackageManifestDto
{
    public const string CurrentFormat = "d2creator-package/1";

    public string Format { get; set; } = CurrentFormat;
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public PackageProjectInfo Project { get; set; } = new();
    public PackageVersionInfo Version { get; set; } = new();
    /// <summary>Plik szablonu w paczce; <c>null</c>, gdy wersja nie ma wgranego szablonu.</summary>
    public PackageTemplateInfo? Template { get; set; }
    public PackageWorkersInfo Workers { get; set; } = new();
}

public class PackageProjectInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Identyfikator pisma (<c>source</c>) — klucz, po którym aplikacja docelowa rozpoznaje pismo.</summary>
    public string Source { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? ApplicationName { get; set; }
    public int RetentionDays { get; set; }
}

public class PackageVersionInfo
{
    public Guid Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public int Major { get; set; }
    public int Minor { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PackageTemplateInfo
{
    /// <summary>Ścieżka w ZIP-ie, np. <c>template/umowa.docx</c>.</summary>
    public string Entry { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    /// <summary><c>docx</c> | <c>xlsx</c> | inne rozszerzenie.</summary>
    public string Kind { get; set; } = string.Empty;
}

public class PackageWorkersInfo
{
    /// <summary>Ścieżka w ZIP-ie (<c>workers.json</c>).</summary>
    public string Entry { get; set; } = "workers.json";
    public int Count { get; set; }
    /// <summary>Liczba workerów wg typu (np. „Podstaw wartość”: 9).</summary>
    public Dictionary<string, int> ByType { get; set; } = new();
}

/// <summary>Jeden worker w <c>workers.json</c> — konfiguracja jako obiekt JSON (nie string).</summary>
public class PackageWorkerEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkerType { get; set; } = string.Empty;
    public System.Text.Json.JsonElement Config { get; set; }
}

/// <summary>Treść <c>workers.json</c>.</summary>
public class PackageWorkersFile
{
    public string Source { get; set; } = string.Empty;
    public string VersionTag { get; set; } = string.Empty;
    public List<PackageWorkerEntry> Workers { get; set; } = new();
}

/// <summary>Wynik wysyłki paczki do aplikacji docelowej.</summary>
public class PackageDeliveryDto
{
    public Guid ProjectId { get; set; }
    public Guid VersionId { get; set; }
    public string VersionTag { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public int WorkerCount { get; set; }
    public bool HasTemplate { get; set; }
    public DateTime DeliveredAt { get; set; }
    /// <summary>Adres, pod który wysłano paczkę.</summary>
    public string DeliveredTo { get; set; } = string.Empty;
    /// <summary>Odpowiedź aplikacji docelowej (status + skrót treści), do podglądu.</summary>
    public string? TargetResponse { get; set; }
}
