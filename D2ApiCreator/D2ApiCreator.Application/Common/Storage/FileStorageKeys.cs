namespace D2ApiCreator.Application.Common.Storage;

/// <summary>
/// Konwencja nazw obiektów w storage. Klucz trafia do <c>files.storage_ref</c>,
/// więc zmiana konwencji nie może unieważnić istniejących wierszy.
/// </summary>
public static class FileStorageKeys
{
    /// <summary>
    /// Szablon wgrany do wersji projektu: <c>templates/{projectVersionId}/{fileId}</c>.
    /// </summary>
    public static string Template(Guid projectVersionId, Guid fileId)
        => $"templates/{projectVersionId}/{fileId}";
}
