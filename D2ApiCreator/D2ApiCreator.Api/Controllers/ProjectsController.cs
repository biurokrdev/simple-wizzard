using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Projects.Commands.CreateProject;
using D2ApiCreator.Application.Features.Projects.Commands.CreateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.UpdateProject;
using D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersionStepData;
using D2ApiCreator.Application.Features.Projects.Commands.ActivateProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.CloneProjectVersion;
using D2ApiCreator.Application.Features.Projects.Commands.DeleteProjectVersion;
using D2ApiCreator.Application.Features.Projects.Queries.GetProjects;
using D2ApiCreator.Application.Features.Packages;
using D2ApiCreator.Application.DTOs.Packages;
using D2ApiCreator.Application.Features.Projects.Queries.GetProject;
using D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersion;
using D2ApiCreator.Application.Features.Projects.Queries.GetProjectVersions;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Controller for project and project version management
/// </summary>
public class ProjectsController : BaseApiController
{
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(ILogger<ProjectsController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all projects with pagination and filtering
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="source">Optional source filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchTerm">Search term for name or source</param>
    /// <param name="status">Filter by status</param>
    /// <param name="dateFrom">Filter by creation date from</param>
    /// <param name="dateTo">Filter by creation date to</param>
    /// <param name="corporateKey">Current user corporate key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of projects</returns>
    /// <response code="200">Projects retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? source,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? corporateKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting projects for user: {CorporateKey}, page: {PageNumber}, size: {PageSize}", 
            corporateKey, pageNumber, pageSize);

        var query = new GetProjectsQuery
        {
            TenantId = tenantId,
            Source = source,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            CurrentUserCorporateKey = corporateKey
        };

        var result = await Mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    /// <param name="dto">Project creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created project</returns>
    /// <response code="201">Project created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Project with this source already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating project for tenant: {TenantId}", dto.TenantId);

        var command = new CreateProjectCommand
        {
            TenantId = dto.TenantId,
            Source = dto.Source,
            Name = dto.Name,
            ApplicationName = dto.ApplicationName,
            RecipientId = dto.RecipientId,
            RetentionDays = dto.RetentionDays,
            Environments = dto.Environments,
            ProcessName = dto.ProcessName,
            ProcessOwner = dto.ProcessOwner,
            BusinessLineId = dto.BusinessLineId,
            ProcessVersion = dto.ProcessVersion,
            ProcessStep = dto.ProcessStep,
            StatusId = dto.StatusId,
            Description = dto.Description,
            CreatedBy = dto.CreatedBy,
            OwnerCorporateKey = dto.OwnerCorporateKey
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(
            nameof(GetProject),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Create a new project version
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="dto">Version creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created project version</returns>
    /// <response code="201">Version created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Project not found</response>
    [HttpPost("{projectId}/versions")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProjectVersion(
        Guid projectId,
        [FromBody] CreateProjectVersionDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating version for project: {ProjectId}", projectId);

        var command = new CreateProjectVersionCommand
        {
            ProjectId = projectId,
            Major = dto.Major,
            Minor = dto.Minor,
            ProcessName = dto.ProcessName,
            BusinessLineId = dto.BusinessLineId,
            TenantIdentifier = dto.TenantIdentifier,
            ProcessOwner = dto.ProcessOwner,
            ProcessVersion = dto.ProcessVersion,
            ProcessStep = dto.ProcessStep,
            StatusId = dto.StatusId,
            StepData = dto.StepData,
            Notes = dto.Notes,
            CreatedBy = dto.CreatedBy
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(
            nameof(GetProjectVersion),
            new { projectId, versionId = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Get project by ID
    /// </summary>
    /// <param name="id">Project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project details</returns>
    /// <response code="200">Project retrieved successfully</response>
    /// <response code="404">Project not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project: {ProjectId}", id);

        var query = new GetProjectQuery { Id = id };
        var result = await Mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all versions for a project
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of project versions</returns>
    /// <response code="200">Versions retrieved successfully</response>
    [HttpGet("{projectId}/versions")]
    [ProducesResponseType(typeof(List<ProjectVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectVersions(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting versions for project: {ProjectId}", projectId);

        var query = new GetProjectVersionsQuery { ProjectId = projectId };
        var result = await Mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get project version by ID
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project version details</returns>
    /// <response code="200">Version retrieved successfully</response>
    /// <response code="404">Project or version not found</response>
    [HttpGet("{projectId}/versions/{versionId}")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectVersion(
        Guid projectId, 
        Guid versionId, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project version. ProjectId: {ProjectId}, VersionId: {VersionId}", 
            projectId, versionId);

        var query = new GetProjectVersionQuery 
        { 
            ProjectId = projectId, 
            VersionId = versionId 
        };
        
        var result = await Mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Paczka wdrożeniowa wersji (BR-022): ZIP z manifest.json, workers.json (wszystkie workery) i plikiem szablonu wgranym w tej wersji.
    /// </summary>
    [HttpGet("{projectId}/versions/{versionId}/package")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPackage(Guid projectId, Guid versionId, [FromQuery] string? requestedBy, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new BuildProjectPackageQuery(projectId, versionId, requestedBy), cancellationToken);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.Error?.Contains("not found") == true ? NotFound(new { message = result.Error }) : BadRequest(new { message = result.Error });
        }
        Response.Headers["X-Package-Sha256"] = result.Value.Sha256;
        return File(result.Value.Content, ProjectPackage.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Buduje paczkę wersji i wysyła ją do aplikacji docelowej (TargetApplication:PackageUrl); zapisuje datę/cel/skrót na wersji.
    /// </summary>
    [HttpPost("{projectId}/versions/{versionId}/package/publish")]
    [ProducesResponseType(typeof(PackageDeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> PublishPackage(Guid projectId, Guid versionId, [FromBody] PublishPackageRequest? body, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new PublishProjectPackageCommand(projectId, versionId, body?.SentBy), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        if (result.Error?.Contains("not found") == true) return NotFound(new { message = result.Error });
        if (result.Error?.StartsWith("Target application rejected") == true) return StatusCode(StatusCodes.Status502BadGateway, new { message = result.Error });
        return BadRequest(new { message = result.Error });
    }

    /// <summary>Kto wysyła paczkę (do manifestu; do czasu R-03 podaje GUI).</summary>
    public record PublishPackageRequest(string? SentBy);

    /// <summary>
    /// Update an existing project
    /// </summary>
    /// <param name="id">Project identifier</param>
    /// <param name="dto">Project update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated project</returns>
    /// <response code="200">Project updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Project not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(
        Guid id,
        [FromBody] UpdateProjectDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating project: {ProjectId}", id);

        var command = new UpdateProjectCommand
        {
            Id = id,
            Name = dto.Name,
            DefaultTemplateName = dto.DefaultTemplateName,
            Description = dto.Description,
            RetentionDays = dto.RetentionDays
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update an existing project version
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="dto">Version update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated project version</returns>
    /// <response code="200">Version updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Version not found</response>
    [HttpPut("{projectId}/versions/{versionId}")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProjectVersion(
        Guid projectId,
        Guid versionId,
        [FromBody] UpdateProjectVersionDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating version {VersionId} for project {ProjectId}", versionId, projectId);

        var command = new UpdateProjectVersionCommand
        {
            Id = versionId,
            StepData = dto.StepData,
            Notes = dto.Notes
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activate a specific project version
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Version identifier to activate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activated project version</returns>
    /// <response code="200">Version activated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Project or version not found</response>
    /// <summary>
    /// Creates a new draft version as a copy of an existing one (step data, current template file, workers).
    /// Used by the GUI when the editor wants to change a version that is already active — the active
    /// version stays untouched and the editor continues on the copy (next minor number, BR-003).
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Source version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new draft version</returns>
    /// <response code="201">New draft version created</response>
    /// <response code="400">Version does not belong to the project</response>
    /// <response code="404">Project or version not found</response>
    [HttpPost("{projectId}/versions/{versionId}/clone")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneProjectVersion(
        Guid projectId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cloning version {VersionId} of project {ProjectId} into a new draft", versionId, projectId);

        var result = await Mediator.Send(
            new CloneProjectVersionCommand { ProjectId = projectId, SourceVersionId = versionId },
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(
            nameof(GetProjectVersion),
            new { projectId, versionId = result.Value!.Id, version = "1.0" },
            result.Value);
    }

    [HttpPost("{projectId}/versions/{versionId}/activate")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProjectVersion(
        Guid projectId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating version {VersionId} for project {ProjectId}", versionId, projectId);

        var command = new ActivateProjectVersionCommand
        {
            ProjectId = projectId,
            VersionId = versionId
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update project version step data
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="dto">Step data update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Step data updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Version not found</response>
    [HttpPatch("{projectId}/versions/{versionId}/step-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProjectVersionStepData(
        Guid projectId,
        Guid versionId,
        [FromBody] UpdateProjectVersionStepDataDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating step data for version {VersionId} of project {ProjectId}", versionId, projectId);

        var command = new UpdateProjectVersionStepDataCommand
        {
            ProjectVersionId = versionId,
            StepData = dto.StepData,
            Notes = dto.Notes
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Step data updated successfully" });
    }

    /// <summary>
    /// Delete a specific project version
    /// </summary>
    /// <param name="projectId">Project identifier</param>
    /// <param name="versionId">Version identifier to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Version deleted successfully</response>
    /// <response code="400">Cannot delete active version</response>
    /// <response code="404">Project or version not found</response>
    [HttpDelete("{projectId}/versions/{versionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProjectVersion(
        Guid projectId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting version {VersionId} from project {ProjectId}", versionId, projectId);

        var command = new DeleteProjectVersionCommand
        {
            ProjectId = projectId,
            VersionId = versionId
        };

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }
}
