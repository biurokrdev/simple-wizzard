using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Tenants;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Queries.GetTenants;

/// <summary>
/// Query to get tenants for current user
/// </summary>
public class GetTenantsQuery : IRequest<Result<List<TeamDto>>>
{
    /// <summary>
    /// Corporate key of the requesting user
    /// </summary>
    public string CurrentUserCorporateKey { get; set; } = string.Empty;
}
