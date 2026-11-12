namespace D2ApiCreator.Application.Common.Interfaces;

/// <summary>
/// Abstrakcja przechowywania plików binarnych (szablony DOCX) poza bazą danych.
/// Implementacja produkcyjna: Google Cloud Storage; DEV/test: fake-gcs-server.
/// Baza trzyma wyłącznie metadane i klucz obiektu (<c>files.storage_ref</c>).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Zapisuje plik pod podaną nazwą obiektu i zwraca tę nazwę (klucz do <c>storage_ref</c>).
    /// Idempotentny: ponowny zapis tej samej nazwy nadpisuje obiekt.
    /// </summary>
    Task<string> UploadAsync(string objectName, byte[] content, string? contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera plik binarny ze storage.
    /// </summary>
    Task<byte[]> DownloadAsync(string storageRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa plik ze storage. Brak obiektu nie jest błędem.
    /// </summary>
    Task DeleteAsync(string storageRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy plik istnieje w storage.
    /// </summary>
    Task<bool> ExistsAsync(string storageRef, CancellationToken cancellationToken = default);
}
