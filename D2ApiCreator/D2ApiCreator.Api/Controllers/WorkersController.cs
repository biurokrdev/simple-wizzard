using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Application.Features.Workers.Commands.GenerateWorkers;
using D2ApiCreator.Application.Features.Workers.Commands.CreateWorker;
using D2ApiCreator.Application.Features.Workers.Commands.UpdateWorker;
using D2ApiCreator.Application.Features.Workers.Commands.DeleteWorker;
using D2ApiCreator.Application.Features.Workers.Queries.GetWorkersByProjectVersion;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Controller for worker management (field mapping configuration)
/// </summary>
public class WorkersController : BaseApiController
{
    private readonly ILogger<WorkersController> _logger;

    public WorkersController(ILogger<WorkersController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate workers from document placeholders and content controls
    /// </summary>
    /// <param name="dto">Generation request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated workers</returns>
    /// <remarks>
    /// Creates workers automatically:
    /// - "Podstaw wartość" workers for placeholders (e.g., &lt;%data_aktualna%&gt;)
    /// - "Stan" workers for text content controls
    /// 
    /// Example request:
    /// {
    ///   "projectVersionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "placeholders": ["&lt;%data_aktualna%&gt;", "&lt;%porto%&gt;"],
    ///   "contentControls": {
    ///     "tag_pokaz_rozszerzony_regulamin": "boolean"
    ///   }
    /// }
    /// </remarks>
    /// <response code="200">Workers generated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Project version not found</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(List<WorkerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateWorkers(
        [FromBody] GenerateWorkersDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating workers for project version {ProjectVersionId}",
                dto.ProjectVersionId);

            var command = new GenerateWorkersCommand(dto);
            var result = await Mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project version not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating workers");
            return StatusCode(500, new { message = "An error occurred while generating workers" });
        }
    }

    /// <summary>
    /// Get all workers for a project version
    /// </summary>
    /// <param name="projectVersionId">Project version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of workers</returns>
    /// <response code="200">Workers retrieved successfully</response>
    [HttpGet("project-version/{projectVersionId}")]
    [ProducesResponseType(typeof(List<WorkerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkersByProjectVersion(
        Guid projectVersionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting workers for project version {ProjectVersionId}",
            projectVersionId);

        var query = new GetWorkersByProjectVersionQuery(projectVersionId);
        var result = await Mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Create a new worker manually
    /// </summary>
    /// <param name="dto">Worker creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created worker</returns>
    /// <response code="201">Worker created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Project version not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateWorker(
        [FromBody] CreateWorkerDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateWorkerCommand(dto);
            var result = await Mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetWorkersByProjectVersion), 
                new { projectVersionId = result.ProjectVersionId }, 
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating worker");
            return StatusCode(500, new { message = "An error occurred while creating worker" });
        }
    }

    /// <summary>
    /// Update an existing worker
    /// </summary>
    /// <param name="id">Worker ID</param>
    /// <param name="dto">Worker update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated worker</returns>
    /// <response code="200">Worker updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Worker not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorker(
        Guid id,
        [FromBody] UpdateWorkerDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateWorkerCommand(id, dto);
            var result = await Mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating worker");
            return StatusCode(500, new { message = "An error occurred while updating worker" });
        }
    }

    /// <summary>
    /// Delete a worker
    /// </summary>
    /// <param name="id">Worker ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Worker deleted successfully</response>
    /// <response code="404">Worker not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorker(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteWorkerCommand(id);
        var result = await Mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { message = "Worker not found" });
        }

        return NoContent();
    }
}
