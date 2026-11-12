using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Tenants;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant
/// </summary>
public class CreateTenantCommand : IRequest<Result<TeamDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> MemberCorporateKeys { get; set; } = new();
    public Guid CreatedBy { get; set; }
    public string CreatedByCorporateKey { get; set; } = string.Empty;
}
