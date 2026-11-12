using D2ApiCreator.Application.DTOs.Packages;

namespace D2ApiCreator.Application.Common.Interfaces;

/// <summary>Odpowiedź aplikacji docelowej na wysyłkę paczki.</summary>
public record PackageDeliveryResult(bool Success, string Target, string? ResponseSummary, string? Error);

/// <summary>
/// Wysyłka paczki wdrożeniowej (ZIP) do aplikacji docelowej. Implementacja HTTP wymaga adresu
/// w konfiguracji (<c>TargetApplication:PackageUrl</c>); bez niego <see cref="IsConfigured"/> = false
/// i wysyłka kończy się czytelnym błędem (paczkę nadal można pobrać).
/// </summary>
public interface IPackageDeliveryService
{
    bool IsConfigured { get; }

    /// <summary>Opis celu (adres) do komunikatów i zapisu na wersji.</summary>
    string Target { get; }

    Task<PackageDeliveryResult> SendAsync(ProjectPackage package, CancellationToken cancellationToken = default);
}
