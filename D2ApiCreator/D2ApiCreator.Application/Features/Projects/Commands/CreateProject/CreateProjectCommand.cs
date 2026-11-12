using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Projects;
using MediatR;

namespace D2ApiCreator.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Command to create a new project
/// </summary>
public record CreateProjectCommand : IRequest<Result<ProjectDto>>
{
    public string? TenantId { get; init; }
    public string? Source { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ApplicationName { get; init; } = string.Empty;
    public string? RecipientId { get; init; }
    public int RetentionDays { get; init; } = 90;
    public List<string> Environments { get; init; } = new();
    public string? ProcessName { get; init; }
    public string? ProcessOwner { get; init; }
    public string? BusinessLineId { get; init; }
    public string? ProcessVersion { get; init; }
    public string? ProcessStep { get; init; }
    public string? StatusId { get; init; }
    public string? Description { get; init; }
    public Guid? CreatedBy { get; init; }
    public string? OwnerCorporateKey { get; init; }
}
