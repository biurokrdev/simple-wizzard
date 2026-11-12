using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using D2ApiCreator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D2ApiCreator.Infrastructure.Services.WorkerCatalog;

/// <summary>Sekcja <c>WorkerCatalog</c> w appsettings.</summary>
public class WorkerCatalogOptions
{
    public const string SectionName = "WorkerCatalog";

    /// <summary>Adres listy typów workerów aplikacji docelowej (GET → JSON). Pusty = katalog wbudowany.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Opcjonalny klucz API wysyłany w nagłówku <c>X-Api-Key</c>.</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 15;
}

/// <summary>
/// Katalog wbudowany — używany, gdy adres katalogu aplikacji docelowej nie jest skonfigurowany (R-31).
/// Zawiera typy, które kreator dziś generuje (BR-010/011), oraz typy znane z aplikacji docelowej,
/// które administrator może udostępnić użytkownikom (np. usuwanie obszaru, gdy warunek jest fałszywy).
/// </summary>
public class StaticWorkerCatalogSource : IWorkerCatalogSource
{
    public string Name => "wbudowany katalog (WorkerCatalog:BaseUrl nie ustawiony)";

    public Task<IReadOnlyList<WorkerCatalogItem>> GetWorkerTypesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkerCatalogItem> items = new[]
        {
            new WorkerCatalogItem("Podstaw wartość", "Podstaw wartość",
                "Podmienia znacznik <%…%> na wartość z modelu danych (formModel).",
                """{"search_for":"<%{name}%>","replacement_property_expression":"formModel.{name}"}"""),
            new WorkerCatalogItem("Stan", "Stan",
                "Pokazuje lub ukrywa kontrolkę tekstową zależnie od warunku logicznego.",
                """{"tag_name":"{name}","conditional_expression":"formModel.{name}"}"""),
            new WorkerCatalogItem("Usuń obszar", "Usuń obszar",
                "Usuwa z dokumentu obszar oznaczony tagiem, gdy warunek jest fałszywy — dopełnienie workera „Stan”.",
                """{"tag_name":"{name}","conditional_expression":"formModel.{name}","remove_when":false}"""),
            new WorkerCatalogItem("Powtórz blok", "Powtórz blok",
                "Powiela obszar oznaczony tagiem dla każdego elementu kolekcji z modelu danych.",
                """{"tag_name":"{name}","collection_expression":"formModel.{name}"}"""),
            new WorkerCatalogItem("Wstaw obraz", "Wstaw obraz",
                "Wstawia obraz (np. podpis, kod QR) w miejsce znacznika.",
                """{"search_for":"<%{name}%>","image_property_expression":"formModel.{name}"}"""),
            new WorkerCatalogItem("Formatuj datę", "Formatuj datę",
                "Podstawia datę z modelu w zadanym formacie.",
                """{"search_for":"<%{name}%>","replacement_property_expression":"formModel.{name}","format":"dd.MM.yyyy"}"""),
        };
        return Task.FromResult(items);
    }
}

/// <summary>
/// Katalog z HTTP: GET <c>BaseUrl</c> zwraca tablicę obiektów z polami <c>key</c>/<c>name</c>/<c>description</c>/<c>defaultConfig</c>
/// (albo <c>workerType</c>/<c>displayName</c>/<c>defaultConfigJson</c> — oba warianty są akceptowane).
/// </summary>
public class HttpWorkerCatalogSource : IWorkerCatalogSource
{
    private readonly HttpClient _httpClient;
    private readonly WorkerCatalogOptions _options;
    private readonly ILogger<HttpWorkerCatalogSource> _logger;

    public HttpWorkerCatalogSource(HttpClient httpClient, IOptions<WorkerCatalogOptions> options, ILogger<HttpWorkerCatalogSource> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => _options.BaseUrl ?? "(brak adresu)";

    public async Task<IReadOnlyList<WorkerCatalogItem>> GetWorkerTypesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.BaseUrl);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<CatalogEntry>>(cancellationToken: cancellationToken) ?? new List<CatalogEntry>();
        _logger.LogInformation("Worker catalog {Url} returned {Count} types", _options.BaseUrl, items.Count);

        return items
            .Select(e => new WorkerCatalogItem(
                e.Key ?? e.WorkerType ?? string.Empty,
                e.Name ?? e.DisplayName ?? e.Key ?? e.WorkerType ?? string.Empty,
                e.Description,
                e.DefaultConfig?.ValueKind == JsonValueKind.Object ? e.DefaultConfig.Value.GetRawText() : e.DefaultConfigJson))
            .Where(i => i.Key.Length > 0)
            .ToList();
    }

    private sealed class CatalogEntry
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("workerType")] public string? WorkerType { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("defaultConfig")] public JsonElement? DefaultConfig { get; set; }
        [JsonPropertyName("defaultConfigJson")] public string? DefaultConfigJson { get; set; }
    }
}
