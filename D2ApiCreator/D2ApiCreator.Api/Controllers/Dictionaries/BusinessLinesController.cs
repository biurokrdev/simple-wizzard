using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Kontroler słownikowy dla linii biznesowych z systemu zewnętrznego
/// </summary>
public class BusinessLinesController : BaseDictionaryController
{
    private readonly IExternalDataService _externalDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BusinessLinesController> _logger;
    private const string CacheKey = "external_business_lines";

    public BusinessLinesController(
        IExternalDataService externalDataService,
        ICacheService cacheService,
        ILogger<BusinessLinesController> logger)
    {
        _externalDataService = externalDataService ?? throw new ArgumentNullException(nameof(externalDataService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pobiera listę aktywnych linii biznesowych z cache lub systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista aktywnych linii biznesowych</returns>
    /// <response code="200">Lista linii biznesowych została pomyślnie pobrana</response>
    /// <response code="500">Wystąpił błąd podczas pobierania listy linii biznesowych</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<BusinessLineDto>>> GetBusinessLines(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching business lines list from dictionary");
            var businessLines = await _externalDataService.GetBusinessLinesAsync(cancellationToken);
            
            // Filtruj tylko aktywne
            var activeBusinessLines = businessLines.Where(bl => bl.IsActive).ToList();
            
            _logger.LogInformation("Successfully fetched {Count} active business lines out of {Total}", 
                activeBusinessLines.Count, businessLines.Count);
            return Ok(activeBusinessLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching business lines from dictionary");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while fetching business lines" });
        }
    }

    /// <summary>
    /// Odświeża cache linii biznesowych - wymusza ponowne pobranie danych z systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowana lista aktywnych linii biznesowych</returns>
    /// <response code="200">Cache został odświeżony i zwrócono zaktualizowaną listę</response>
    /// <response code="500">Wystąpił błąd podczas odświeżania cache</response>
    [HttpPut("cache")]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<BusinessLineDto>>> UpdateCache(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing business lines dictionary cache");
            
            var businessLines = await _cacheService.RefreshAsync(
                CacheKey,
                () => _externalDataService.GetBusinessLinesAsync(cancellationToken),
                TimeSpan.FromHours(24)
            );
            
            var activeBusinessLines = businessLines.Where(bl => bl.IsActive).ToList();
            
            _logger.LogInformation("Successfully refreshed business lines cache with {Count} active items", 
                activeBusinessLines.Count);
            return Ok(activeBusinessLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing business lines cache");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while refreshing business lines cache" });
        }
    }
}
