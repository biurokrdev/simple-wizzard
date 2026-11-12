using D2ApiCreator.Application.DTOs.Tenants;
using D2ApiCreator.Application.Features.Tenants.Commands.AddTenantMember;
using D2ApiCreator.Application.Features.Tenants.Commands.CreateTenant;
using D2ApiCreator.Application.Features.Tenants.Commands.DeleteTenant;
using D2ApiCreator.Application.Features.Tenants.Commands.RemoveTenantMember;
using D2ApiCreator.Application.Features.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IMediator mediator, ILogger<TenantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all teams where the current user is a member
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants([FromQuery] string corporateKey)
    {
        var query = new GetTenantsQuery
        {
            CurrentUserCorporateKey = corporateKey
        };

        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, [FromQuery] Guid createdBy, [FromQuery] string corporateKey)
    {
        var command = new CreateTenantCommand
        {
            Name = request.Name,
            Description = request.Description,
            MemberCorporateKeys = request.MemberCorporateKeys,
            CreatedBy = createdBy,
            CreatedByCorporateKey = corporateKey
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetTenants), new { corporateKey }, result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Add a member to tenant
    /// </summary>
    [HttpPost("{tenantId}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMember(Guid tenantId, [FromBody] AddTenantMemberRequest request, [FromQuery] Guid addedBy, [FromQuery] string requesterCorporateKey)
    {
        var command = new AddTenantMemberCommand
        {
            TenantId = tenantId,
            CorporateKey = request.CorporateKey,
            AddedBy = addedBy,
            RequesterCorporateKey = requesterCorporateKey
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Remove a member from tenant
    /// </summary>
    [HttpDelete("{tenantId}/members/{memberId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(Guid tenantId, Guid memberId, [FromQuery] Guid requesterId, [FromQuery] string requesterCorporateKey)
    {
        var command = new RemoveTenantMemberCommand
        {
            TenantId = tenantId,
            MemberId = memberId,
            RequesterId = requesterId,
            RequesterCorporateKey = requesterCorporateKey
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Delete a tenant (only if no projects are associated)
    /// </summary>
    [HttpDelete("{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTenant(Guid tenantId, [FromQuery] Guid requesterId)
    {
        var command = new DeleteTenantCommand
        {
            TenantId = tenantId,
            RequesterId = requesterId
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(result.Error);
    }
}
