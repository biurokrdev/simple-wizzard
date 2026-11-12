namespace D2ApiCreator.Application.Common.Interfaces;

/// <summary>Pozycja katalogu typów workerów aplikacji docelowej.</summary>
/// <param name="Key">Nazwa typu (zapisywana w <c>workers.worker_type</c>).</param>
/// <param name="DisplayName">Nazwa do wyświetlenia (gdy katalog jej nie podaje — równa <paramref name="Key"/>).</param>
/// <param name="Description">Opis działania.</param>
/// <param name="DefaultConfigJson">Domyślna konfiguracja (JSON) albo <c>null</c>.</param>
public sealed record WorkerCatalogItem(string Key, string DisplayName, string? Description, string? DefaultConfigJson);

/// <summary>
/// Źródło katalogu typów workerów — aplikacja docelowa, do której trafia parametryzacja pisma
/// (ma komplet workerów; kreator pokazuje użytkownikom tylko te, które wybierze administrator).
/// </summary>
public interface IWorkerCatalogSource
{
    /// <summary>Nazwa źródła do logów i wyniku synchronizacji (np. adres API albo „wbudowany”).</summary>
    string Name { get; }

    Task<IReadOnlyList<WorkerCatalogItem>> GetWorkerTypesAsync(CancellationToken cancellationToken = default);
}
