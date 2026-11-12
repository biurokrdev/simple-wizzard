using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects.Commands.ActivateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.DeactivateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Queries.GetProject;
using D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersions;
using D2ServicesCreator.Api.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace D2ServicesCreator.Api.Controllers;

/// <summary>
/// Minimal project API for external systems: read the status of a letter template and switch its
/// versions on or off. Everything else (creating projects, wizard steps, files, workers, tenants)
/// is intentionally not reachable from here — that surface belongs to the GUI API (D2ApiCreator).
/// </summary>
// No [Produces("application/json")] here on purpose: ProducesAttribute overwrites ObjectResult.ContentTypes,
// which would turn the RFC 7807 error responses below back into plain application/json.
[ApiController]
[Route("api/v1/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(ISender sender, ILogger<ProjectsController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Returns the status of a project (letter template): whether it is active, which version is
    /// active and the list of all versions. The response is deliberately narrowed — the wizard's
    /// <c>stepData</c>, project metadata and retention settings are not exposed.
    /// </summary>
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectStatus(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var projectResult = await _sender.Send(new GetProjectQuery { Id = projectId }, cancellationToken);
        if (!projectResult.IsSuccess)
            return Problem(StatusCodes.Status404NotFound, "Not Found", projectResult.Error);

        var versionsResult = await _sender.Send(new GetProjectVersionsQuery { ProjectId = projectId }, cancellationToken);
        if (!versionsResult.IsSuccess)
        {
            _logger.LogError("Reading versions of project {ProjectId} failed: {Error}", projectId, versionsResult.Error);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "Could not read project versions.");
        }

        return Ok(ProjectStatusResponse.From(projectResult.Value!, versionsResult.Value!));
    }

    /// <summary>
    /// Activates the given version of a project. Any other active version of the project is
    /// deactivated and <c>projects.current_active_version_id</c> is moved to this version (BR-004).
    /// </summary>
    [HttpPost("{projectId:guid}/versions/{versionId:guid}/activate")]
    [ProducesResponseType(typeof(ProjectVersionStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateVersion(
        [FromRoute] Guid projectId,
        [FromRoute] Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ActivateProjectVersionCommand { ProjectId = projectId, VersionId = versionId },
            cancellationToken);

        if (result.IsSuccess)
            _logger.LogInformation("External activation of version {VersionId} for project {ProjectId}", versionId, projectId);

        return MapVersionResult(result);
    }

    /// <summary>
    /// Deactivates the given version of a project. If it was the active one, the project is left
    /// without an active version — no other version is activated implicitly. Idempotent: deactivating
    /// an already inactive version returns 200 with the unchanged state.
    /// </summary>
    [HttpPost("{projectId:guid}/versions/{versionId:guid}/deactivate")]
    [ProducesResponseType(typeof(ProjectVersionStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateVersion(
        [FromRoute] Guid projectId,
        [FromRoute] Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateProjectVersionCommand { ProjectId = projectId, VersionId = versionId },
            cancellationToken);

        if (result.IsSuccess)
            _logger.LogInformation("External deactivation of version {VersionId} for project {ProjectId}", versionId, projectId);

        return MapVersionResult(result);
    }

    // Shared handlers signal "missing" through Result.Error text (same convention as D2ApiCreator's
    // ProjectsController). A version of another project is reported as 404 too — from the caller's
    // point of view there is no such version under that project.
    private IActionResult MapVersionResult(Result<ProjectVersionDto> result)
    {
        if (result.IsSuccess)
            return Ok(ProjectVersionStateResponse.From(result.Value!));

        var isNotFound = result.Error is not null
            && (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || result.Error.Contains("does not belong", StringComparison.OrdinalIgnoreCase));

        return isNotFound
            ? Problem(StatusCodes.Status404NotFound, "Not Found", result.Error)
            : Problem(StatusCodes.Status400BadRequest, "Bad Request", result.Error);
    }

    // ControllerBase.Problem() needs ProblemDetailsFactory from the MVC services; building the
    // object directly keeps the controller unit-testable without a host.
    private static ObjectResult Problem(int statusCode, string title, string? detail) =>
        new(new ProblemDetails { Status = statusCode, Title = title, Detail = detail })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
}
