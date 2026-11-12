using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Kontroler słownikowy dla odbiorców dokumentów z systemu zewnętrznego
/// </summary>
public class RecipientsController : BaseDictionaryController
{
    private readonly IExternalDataService _externalDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RecipientsController> _logger;
    private const string CacheKey = "external_recipients";

    public RecipientsController(
        IExternalDataService externalDataService,
        ICacheService cacheService,
        ILogger<RecipientsController> logger)
    {
        _externalDataService = externalDataService ?? throw new ArgumentNullException(nameof(externalDataService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pobiera listę aktywnych odbiorców z cache lub systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista aktywnych odbiorców</returns>
    /// <response code="200">Lista odbiorców została pomyślnie pobrana</response>
    /// <response code="500">Wystąpił błąd podczas pobierania listy odbiorców</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RecipientDto>>> GetRecipients(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching recipients list from dictionary");
            var recipients = await _externalDataService.GetRecipientsAsync(cancellationToken);
            
            // Filtruj tylko aktywnych
            var activeRecipients = recipients.Where(r => r.IsActive).ToList();
            
            _logger.LogInformation("Successfully fetched {Count} active recipients out of {Total}", 
                activeRecipients.Count, recipients.Count);
            return Ok(activeRecipients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching recipients from dictionary");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while fetching recipients" });
        }
    }

    /// <summary>
    /// Odświeża cache odbiorców - wymusza ponowne pobranie danych z systemu zewnętrznego
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowana lista aktywnych odbiorców</returns>
    /// <response code="200">Cache został odświeżony i zwrócono zaktualizowaną listę</response>
    /// <response code="500">Wystąpił błąd podczas odświeżania cache</response>
    [HttpPut("cache")]
    [ProducesResponseType(typeof(IReadOnlyList<RecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RecipientDto>>> UpdateCache(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing recipients dictionary cache");
            
            var recipients = await _cacheService.RefreshAsync(
                CacheKey,
                () => _externalDataService.GetRecipientsAsync(cancellationToken),
                TimeSpan.FromHours(24)
            );
            
            var activeRecipients = recipients.Where(r => r.IsActive).ToList();
            
            _logger.LogInformation("Successfully refreshed recipients cache with {Count} active items", 
                activeRecipients.Count);
            return Ok(activeRecipients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing recipients cache");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while refreshing recipients cache" });
        }
    }
}
