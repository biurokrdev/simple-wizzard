using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Projects.Commands.UpdateProjectVersion;

/// <summary>
/// Command to update an existing project version
/// </summary>
public record UpdateProjectVersionCommand : IRequest<Result<ProjectVersionDto>>
{
    public Guid Id { get; init; }
    public JsonDocument? StepData { get; init; }
    public string? Notes { get; init; }
}
