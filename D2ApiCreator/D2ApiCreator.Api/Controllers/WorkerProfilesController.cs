using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Application.Features.WorkerProfiles.Commands;
using D2ApiCreator.Application.Features.WorkerProfiles.Queries;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Profile workerów (katalog typów dla kreatora, BR-016). Operacje poza <c>GET /visible</c> są
/// administracyjne — do czasu wdrożenia uwierzytelniania (R-03) chroni je tylko brak linku w GUI.
/// </summary>
public class WorkerProfilesController : BaseApiController
{
    private readonly ILogger<WorkerProfilesController> _logger;

    public WorkerProfilesController(ILogger<WorkerProfilesController> logger)
    {
        _logger = logger;
    }

    /// <summary>Wszystkie profile (panel administracyjny).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkerProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWorkerProfilesQuery(OnlyVisible: false), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Profile widoczne dla użytkowników kreatora (dialog mapowania).</summary>
    [HttpGet("visible")]
    [ProducesResponseType(typeof(List<WorkerProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisible(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWorkerProfilesQuery(OnlyVisible: true), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Ręczne dodanie profilu.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkerProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWorkerProfileDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateWorkerProfileCommand(dto), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("already exists") == true
                ? Conflict(new { message = result.Error })
                : BadRequest(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, result.Value);
    }

    /// <summary>Edycja profilu: nazwa, opis, domyślna konfiguracja, widoczność, kolejność.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WorkerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkerProfileDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateWorkerProfileCommand(id, dto), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { message = result.Error })
                : BadRequest(new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>Usunięcie profilu dodanego ręcznie (zsynchronizowane można tylko ukryć).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteWorkerProfileCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { message = result.Error })
                : BadRequest(new { message = result.Error });
        }
        return NoContent();
    }

    /// <summary>Synchronizacja z katalogiem typów aplikacji docelowej (nowe typy trafiają jako ukryte).</summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(WorkerProfileSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker profile sync requested");
        var result = await Mediator.Send(new SyncWorkerProfilesCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = result.Error });
        }
        return Ok(result.Value);
    }
}
