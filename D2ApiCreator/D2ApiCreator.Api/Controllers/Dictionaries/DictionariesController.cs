using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Główny kontroler zarządzający wszystkimi słownikami zewnętrznymi
/// Pozwala na odświeżanie cache pojedynczego lub wszystkich słowników jednocześnie
/// </summary>
[Route("api/v{version:apiVersion}/dictionaries")]
public class DictionariesController : BaseDictionaryController
{
    private readonly IExternalDataService _externalDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DictionariesController> _logger;

    public DictionariesController(
        IExternalDataService externalDataService,
        ICacheService cacheService,
        ILogger<DictionariesController> logger)
    {
        _externalDataService = externalDataService ?? throw new ArgumentNullException(nameof(externalDataService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Odświeża cache wszystkich słowników jednocześnie
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Potwierdzenie odświeżenia wszystkich słowników</returns>
    /// <response code="200">Cache wszystkich słowników został pomyślnie odświeżony</response>
    /// <response code="500">Wystąpił błąd podczas odświeżania cache</response>
    [HttpPut("cache")]
    [ProducesResponseType(typeof(RefreshAllResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefreshAllResult>> UpdateAllCaches(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing all dictionary caches");

            var tasks = new[]
            {
                RefreshRecipients(cancellationToken),
                RefreshBusinessLines(cancellationToken),
                RefreshTenants(cancellationToken),
                RefreshStatuses(cancellationToken)
            };

            var results = await Task.WhenAll(tasks);

            var response = new RefreshAllResult
            {
                Recipients = results[0],
                BusinessLines = results[1],
                Tenants = results[2],
                Statuses = results[3],
                RefreshedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully refreshed all dictionary caches");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing all dictionaries");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while refreshing all dictionaries" });
        }
    }

    private async Task<RefreshResult> RefreshRecipients(CancellationToken cancellationToken)
    {
        var recipients = await _cacheService.RefreshAsync(
            "external_recipients",
            () => _externalDataService.GetRecipientsAsync(cancellationToken),
            TimeSpan.FromHours(24)
        );
        return new RefreshResult 
        { 
            DictionaryName = "recipients", 
            ItemCount = recipients.Count(r => r.IsActive),
            TotalCount = recipients.Count,
            RefreshedAt = DateTime.UtcNow
        };
    }

    private async Task<RefreshResult> RefreshBusinessLines(CancellationToken cancellationToken)
    {
        var businessLines = await _cacheService.RefreshAsync(
            "external_business_lines",
            () => _externalDataService.GetBusinessLinesAsync(cancellationToken),
            TimeSpan.FromHours(24)
        );
        return new RefreshResult 
        { 
            DictionaryName = "businesslines", 
            ItemCount = businessLines.Count(bl => bl.IsActive),
            TotalCount = businessLines.Count,
            RefreshedAt = DateTime.UtcNow
        };
    }

    private async Task<RefreshResult> RefreshTenants(CancellationToken cancellationToken)
    {
        var tenants = await _cacheService.RefreshAsync(
            "external_tenants",
            () => _externalDataService.GetTenantsAsync(cancellationToken),
            TimeSpan.FromHours(24)
        );
        return new RefreshResult 
        { 
            DictionaryName = "tenants", 
            ItemCount = tenants.Count(t => t.IsActive),
            TotalCount = tenants.Count,
            RefreshedAt = DateTime.UtcNow
        };
    }

    private async Task<RefreshResult> RefreshStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _cacheService.RefreshAsync(
            "external_statuses",
            () => _externalDataService.GetStatusesAsync(cancellationToken),
            TimeSpan.FromHours(24)
        );
        return new RefreshResult 
        { 
            DictionaryName = "statuses", 
            ItemCount = statuses.Count(s => s.IsActive),
            TotalCount = statuses.Count,
            RefreshedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Wynik odświeżenia pojedynczego słownika
/// </summary>
public class RefreshResult
{
    public string DictionaryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime RefreshedAt { get; set; }
}

/// <summary>
/// Wynik odświeżenia wszystkich słowników
/// </summary>
public class RefreshAllResult
{
    public RefreshResult Recipients { get; set; } = null!;
    public RefreshResult BusinessLines { get; set; } = null!;
    public RefreshResult Tenants { get; set; } = null!;
    public RefreshResult Statuses { get; set; } = null!;
    public DateTime RefreshedAt { get; set; }
}
