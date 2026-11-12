using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Kontroler słownikowy dla statusów dokumentów z systemu zewnętrznego
/// </summary>
public class StatusesController : BaseDictionaryController
{
    private readonly IExternalDataService _externalDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StatusesController> _logger;
    private const string CacheKey = "external_statuses";

    public StatusesController(
        IExternalDataService externalDataService,
        ICacheService cacheService,
        ILogger<StatusesController> logger)
    {
        _externalDataService = externalDataService ?? throw new ArgumentNullException(nameof(externalDataService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pobiera listę aktywnych statusów z cache lub systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista aktywnych statusów</returns>
    /// <response code="200">Lista statusów została pomyślnie pobrana</response>
    /// <response code="500">Wystąpił błąd podczas pobierania listy statusów</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StatusDto>>> GetStatuses(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching statuses list from dictionary");
            var statuses = await _externalDataService.GetStatusesAsync(cancellationToken);
            
            // Filtruj tylko aktywne
            var activeStatuses = statuses.Where(s => s.IsActive).ToList();
            
            _logger.LogInformation("Successfully fetched {Count} active statuses out of {Total}", 
                activeStatuses.Count, statuses.Count);
            return Ok(activeStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching statuses from dictionary");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while fetching statuses" });
        }
    }

    /// <summary>
    /// Odświeża cache statusów - wymusza ponowne pobranie danych z systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowana lista aktywnych statusów</returns>
    /// <response code="200">Cache został odświeżony i zwrócono zaktualizowaną listę</response>
    /// <response code="500">Wystąpił błąd podczas odświeżania cache</response>
    [HttpPut("cache")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StatusDto>>> UpdateCache(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing statuses dictionary cache");
            
            var statuses = await _cacheService.RefreshAsync(
                CacheKey,
                () => _externalDataService.GetStatusesAsync(cancellationToken),
                TimeSpan.FromHours(24)
            );
            
            var activeStatuses = statuses.Where(s => s.IsActive).ToList();
            
            _logger.LogInformation("Successfully refreshed statuses cache with {Count} active items", 
                activeStatuses.Count);
            return Ok(activeStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing statuses cache");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while refreshing statuses cache" });
        }
    }
}
