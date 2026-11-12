using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Tenants;
using D2ApiCreator.Application.Features.Tenants.Admin;
using D2ApiCreator.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Administracja zespołów (tenantów) — BR-018: przegląd wszystkich zespołów, tworzenie z listą użytkowników,
/// blokada zespołu, blokada użytkownika w zespole, dostęp tylko do odczytu, usuwanie użytkownika.
/// Bez autoryzacji API do czasu R-03 (chroni tylko guard w GUI).
/// </summary>
[ApiController]
[Route("api/v1/admin/tenants")]
public class AdminTenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminTenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class AdminCreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> MemberCorporateKeys { get; set; } = new();
        public Guid? CreatedBy { get; set; }
    }

    public class AdminAddMemberRequest
    {
        public string CorporateKey { get; set; } = string.Empty;
        public string Access { get; set; } = nameof(TenantMemberAccess.ReadWrite);
        public Guid? AddedBy { get; set; }
    }

    public class UpdateMemberRequest
    {
        public bool? IsBlocked { get; set; }
        public string? Access { get; set; }
    }

    public class SetActiveRequest
    {
        public bool IsActive { get; set; }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok((await _mediator.Send(new GetAllTenantsQuery(), cancellationToken)).Value);

    [HttpPost]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AdminCreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdminCreateTenantCommand(request.Name, request.Description, request.MemberCorporateKeys, request.CreatedBy ?? Guid.Empty),
            cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{tenantId}/status")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetActive(Guid tenantId, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
        => Map(await _mediator.Send(new SetTenantActiveCommand(tenantId, request.IsActive), cancellationToken));

    [HttpPost("{tenantId}/members")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMember(Guid tenantId, [FromBody] AdminAddMemberRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TenantMemberAccess>(request.Access, ignoreCase: true, out var access))
        {
            return BadRequest(new { message = "Access must be ReadWrite or ReadOnly" });
        }
        return Map(await _mediator.Send(new AdminAddTenantMemberCommand(tenantId, request.CorporateKey, access, request.AddedBy ?? Guid.Empty), cancellationToken));
    }

    [HttpPut("{tenantId}/members/{memberId}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMember(Guid tenantId, Guid memberId, [FromBody] UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        TenantMemberAccess? access = null;
        if (request.Access != null)
        {
            if (!Enum.TryParse<TenantMemberAccess>(request.Access, ignoreCase: true, out var parsed))
            {
                return BadRequest(new { message = "Access must be ReadWrite or ReadOnly" });
            }
            access = parsed;
        }
        return Map(await _mediator.Send(new UpdateTenantMemberCommand(tenantId, memberId, request.IsBlocked, access), cancellationToken));
    }

    [HttpDelete("{tenantId}/members/{memberId}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(Guid tenantId, Guid memberId, CancellationToken cancellationToken)
        => Map(await _mediator.Send(new AdminRemoveTenantMemberCommand(tenantId, memberId), cancellationToken));

    private IActionResult Map(Result<TeamDto> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return result.Error?.Contains("not found") == true
            ? NotFound(new { message = result.Error })
            : BadRequest(new { message = result.Error });
    }
}
