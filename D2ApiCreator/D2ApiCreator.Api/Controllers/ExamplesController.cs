using Microsoft.AspNetCore.Mvc;
using D2ApiCreator.Application.Features.Examples.Queries;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Example controller demonstrating CQRS pattern
/// </summary>
public class ExamplesController : BaseApiController
{
    /// <summary>
    /// Get example message
    /// </summary>
    /// <param name="name">Your name</param>
    /// <returns>Example response with greeting</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ExampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExample([FromRoute] string name)
    {
        var query = new GetExampleQuery { Name = name };
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get example with query parameter
    /// </summary>
    /// <param name="name">Your name</param>
    /// <returns>Example response with greeting</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ExampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExampleByQuery([FromQuery] string name = "World")
    {
        var query = new GetExampleQuery { Name = name };
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}

