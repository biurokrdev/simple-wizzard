namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Konfiguracja Google Cloud Storage (sekcja <c>GoogleCloudStorage</c> w appsettings / zmiennych środowiskowych).
/// Identyczny kształt jak w D2 ViewerEditor (<c>importer</c>).
/// </summary>
public class GcsStorageOptions
{
    public const string SectionName = "GoogleCloudStorage";

    /// <summary>
    /// Nazwa bucketu GCS. Pusta = storage nieskonfigurowany (upload plików zwróci błąd).
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint API (pusty = produkcyjny GCS; ustawiony = fake-gcs-server dla DEV/testów, np. <c>http://localhost:4443</c>).
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Ścieżka do pliku service account JSON (opcjonalnie; produkcja może używać Workload Identity / ADC).
    /// </summary>
    public string? CredentialPath { get; set; }
}
