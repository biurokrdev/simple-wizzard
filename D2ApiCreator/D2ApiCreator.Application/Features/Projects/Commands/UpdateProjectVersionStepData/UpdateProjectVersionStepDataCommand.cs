using D2ApiCreator.Application.Common;
using MediatR;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersionStepData;

/// <summary>
/// Command to update project version step data
/// </summary>
public class UpdateProjectVersionStepDataCommand : IRequest<Result<bool>>
{
    public Guid ProjectVersionId { get; set; }
    public JsonDocument StepData { get; set; } = null!;
    public string? Notes { get; set; }
}
