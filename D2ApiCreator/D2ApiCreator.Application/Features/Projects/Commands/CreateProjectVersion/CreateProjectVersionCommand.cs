using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Projects.Commands.CreateProjectVersion;

/// <summary>
/// Command to create a new project version with wizard step data
/// </summary>
public record CreateProjectVersionCommand : IRequest<Result<ProjectVersionDto>>
{
    public Guid ProjectId { get; init; }
    public int Major { get; init; } = 1;
    public int Minor { get; init; } = 0;
    public string ProcessName { get; init; } = string.Empty;
    public string? BusinessLineId { get; init; }
    public string? TenantIdentifier { get; init; }
    public string ProcessOwner { get; init; } = string.Empty;
    public string ProcessVersion { get; init; } = string.Empty;
    public string ProcessStep { get; init; } = string.Empty;
    public string StatusId { get; init; } = string.Empty;
    public JsonDocument? StepData { get; init; }
    public string? Notes { get; init; }
    public Guid? CreatedBy { get; init; }
}
