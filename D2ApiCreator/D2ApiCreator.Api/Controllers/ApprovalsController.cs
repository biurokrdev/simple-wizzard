using D2ApiCreator.Application.DTOs.Approvals;
using D2ApiCreator.Application.DTOs.Projects;
using D2ApiCreator.Application.Features.Approvals;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Przegląd i akceptacja wersji pisma (BR-017): projektant zgłasza wersję („Wyślij do weryfikacji”),
/// administrator akceptuje albo odsyła do poprawy. Decyzje administratora bez autoryzacji API do czasu R-03.
/// </summary>
public class ApprovalsController : BaseApiController
{
    /// <summary>Wersje czekające na decyzję (panel administracyjny).</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PendingVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPendingVersionsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Zgłoszenie wersji do publikacji (Draft | RequiresChanges → PendingApproval).</summary>
    [HttpPost("{versionId}/submit")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(Guid versionId, [FromBody] SubmitForReviewDto? dto, CancellationToken cancellationToken)
        => Map(await Mediator.Send(new SubmitVersionForReviewCommand(versionId, dto?.SubmittedBy), cancellationToken));

    /// <summary>Akceptacja (PendingApproval → Approved).</summary>
    [HttpPost("{versionId}/approve")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid versionId, [FromBody] ReviewDecisionDto? dto, CancellationToken cancellationToken)
        => Map(await Mediator.Send(new ApproveVersionCommand(versionId, dto?.Reviewer, dto?.Comment), cancellationToken));

    /// <summary>Odrzucenie z komentarzem (PendingApproval → RequiresChanges, „Wymaga poprawy”).</summary>
    [HttpPost("{versionId}/reject")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid versionId, [FromBody] ReviewDecisionDto dto, CancellationToken cancellationToken)
        => Map(await Mediator.Send(new RejectVersionCommand(versionId, dto.Reviewer, dto.Comment), cancellationToken));

    /// <summary>Uwagi recenzenta do kroków kreatora (zapis w trakcie przeglądu; tylko dla wersji PendingApproval).</summary>
    [HttpPut("{versionId}/notes")]
    [ProducesResponseType(typeof(ProjectVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveNotes(Guid versionId, [FromBody] ReviewNotesDto dto, CancellationToken cancellationToken)
        => Map(await Mediator.Send(new SaveReviewNotesCommand(versionId, dto.Notes ?? new Dictionary<string, string>()), cancellationToken));

    private IActionResult Map(Application.Common.Result<ProjectVersionDto> result)
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
