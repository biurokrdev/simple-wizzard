using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Packages;
using D2ApiCreator.Application.Features.Packages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D2ApiCreator.Infrastructure.Services.Packages;

/// <summary>Sekcja <c>TargetApplication</c> w appsettings — adres odbioru paczek wdrożeniowych.</summary>
public class TargetApplicationOptions
{
    public const string SectionName = "TargetApplication";

    /// <summary>Adres POST (multipart/form-data: <c>file</c> = ZIP, <c>manifest</c> = JSON). Pusty = wysyłka wyłączona.</summary>
    public string? PackageUrl { get; set; }

    /// <summary>Opcjonalny klucz API w nagłówku <c>X-Api-Key</c>.</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Wysyłka paczki do aplikacji docelowej: <c>POST PackageUrl</c> jako multipart/form-data —
/// część <c>file</c> (ZIP, nazwa paczki) i część <c>manifest</c> (ten sam JSON co <c>manifest.json</c>),
/// nagłówki <c>X-Api-Key</c> (gdy ustawiony) i <c>X-Package-Sha256</c>. Format do potwierdzenia z aplikacją docelową (R-35).
/// </summary>
public class HttpPackageDeliveryService : IPackageDeliveryService
{
    private readonly HttpClient _httpClient;
    private readonly TargetApplicationOptions _options;
    private readonly ILogger<HttpPackageDeliveryService> _logger;

    public HttpPackageDeliveryService(HttpClient httpClient, IOptions<TargetApplicationOptions> options, ILogger<HttpPackageDeliveryService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.PackageUrl);

    public string Target => _options.PackageUrl ?? "(brak adresu)";

    public async Task<PackageDeliveryResult> SendAsync(ProjectPackage package, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PackageDeliveryResult(false, Target, null, "TargetApplication:PackageUrl is not configured");
        }

        using var content = new MultipartFormDataContent();
        var zip = new ByteArrayContent(package.Content);
        zip.Headers.ContentType = new MediaTypeHeaderValue(ProjectPackage.ContentType);
        content.Add(zip, "file", package.FileName);
        content.Add(new StringContent(JsonSerializer.Serialize(package.Manifest, ProjectPackageBuilder.JsonOptions), Encoding.UTF8, "application/json"), "manifest");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.PackageUrl) { Content = content };
        request.Headers.Add("X-Package-Sha256", package.Sha256);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var summary = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
            if (!string.IsNullOrWhiteSpace(body))
            {
                var trimmed = body.Trim();
                summary += ": " + (trimmed.Length > 300 ? trimmed[..300] + "…" : trimmed);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new PackageDeliveryResult(false, Target, summary, summary);
            }
            _logger.LogInformation("Package {FileName} accepted by {Target}: {Status}", package.FileName, Target, (int)response.StatusCode);
            return new PackageDeliveryResult(true, Target, summary, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Package {FileName} could not be delivered to {Target}", package.FileName, Target);
            return new PackageDeliveryResult(false, Target, null, ex.Message);
        }
    }
}

/// <summary>Bez adresu aplikacji docelowej: wysyłka niedostępna, pobieranie paczki działa.</summary>
public class NotConfiguredPackageDeliveryService : IPackageDeliveryService
{
    public bool IsConfigured => false;
    public string Target => "(TargetApplication:PackageUrl nie ustawiony)";

    public Task<PackageDeliveryResult> SendAsync(ProjectPackage package, CancellationToken cancellationToken = default)
        => Task.FromResult(new PackageDeliveryResult(false, Target, null, "TargetApplication:PackageUrl is not configured"));
}
