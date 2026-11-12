using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Base API Controller with common functionality
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Asp.Versioning.ApiVersion(1.0)]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(T result)
    {
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    protected IActionResult HandleResult<T>(T? result, Func<T, bool> successCondition) where T : class
    {
        if (result == null)
            return NotFound();

        if (successCondition(result))
            return Ok(result);

        return BadRequest();
    }
}

