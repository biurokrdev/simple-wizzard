using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Kontroler słownikowy dla tenantów z systemu zewnętrznego
/// </summary>
public class TenantsController : BaseDictionaryController
{
    private readonly IExternalDataService _externalDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TenantsController> _logger;
    private const string CacheKey = "external_tenants";

    public TenantsController(
        IExternalDataService externalDataService,
        ICacheService cacheService,
        ILogger<TenantsController> logger)
    {
        _externalDataService = externalDataService ?? throw new ArgumentNullException(nameof(externalDataService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pobiera listę aktywnych tenantów z cache lub systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista aktywnych tenantów</returns>
    /// <response code="200">Lista tenantów została pomyślnie pobrana</response>
    /// <response code="500">Wystąpił błąd podczas pobierania listy tenantów</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetTenants(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching tenants list from dictionary");
            var tenants = await _externalDataService.GetTenantsAsync(cancellationToken);
            
            // Filtruj tylko aktywnych
            var activeTenants = tenants.Where(t => t.IsActive).ToList();
            
            _logger.LogInformation("Successfully fetched {Count} active tenants out of {Total}", 
                activeTenants.Count, tenants.Count);
            return Ok(activeTenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching tenants from dictionary");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while fetching tenants" });
        }
    }

    /// <summary>
    /// Odświeża cache tenantów - wymusza ponowne pobranie danych z systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowana lista aktywnych tenantów</returns>
    /// <response code="200">Cache został odświeżony i zwrócono zaktualizowaną listę</response>
    /// <response code="500">Wystąpił błąd podczas odświeżania cache</response>
    [HttpPut("cache")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> UpdateCache(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing tenants dictionary cache");
            
            var tenants = await _cacheService.RefreshAsync(
                CacheKey,
                () => _externalDataService.GetTenantsAsync(cancellationToken),
                TimeSpan.FromHours(24)
            );
            
            var activeTenants = tenants.Where(t => t.IsActive).ToList();
            
            _logger.LogInformation("Successfully refreshed tenants cache with {Count} active items", 
                activeTenants.Count);
            return Ok(activeTenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing tenants cache");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while refreshing tenants cache" });
        }
    }
}
