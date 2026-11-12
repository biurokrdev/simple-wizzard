using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.Tenants;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Tenants.Queries.GetTenants;

/// <summary>
/// Handler for getting user's tenants
/// </summary>
public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, Result<List<TeamDto>>>
{
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IRepository<TenantMember, Guid> _memberRepository;
    private readonly ILogger<GetTenantsQueryHandler> _logger;

    public GetTenantsQueryHandler(
        IRepository<Tenant, Guid> tenantRepository,
        IRepository<TenantMember, Guid> memberRepository,
        ILogger<GetTenantsQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _logger = logger;
    }

    public async Task<Result<List<TeamDto>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting tenants for user: {CorporateKey}", request.CurrentUserCorporateKey);

            // Get all tenant IDs where user is a member
            var userMemberships = await _memberRepository.FindAsync(
                m => m.CorporateKey == request.CurrentUserCorporateKey && !m.IsBlocked,
                cancellationToken);

            var tenantIds = userMemberships.Select(m => m.TenantId).Distinct().ToList();

            // Get all tenants where user is a member
            // Zablokowany zespół (is_active = false) nie jest widoczny dla jego członków (BR-018)
            var tenants = await _tenantRepository.FindAsync(
                t => tenantIds.Contains(t.Id) && t.IsActive,
                cancellationToken);

            // Get all members for these tenants
            var allMembers = await _memberRepository.FindAsync(
                m => tenantIds.Contains(m.TenantId),
                cancellationToken);

            var membersByTenant = allMembers.GroupBy(m => m.TenantId).ToDictionary(g => g.Key, g => g.ToList());

            var tenantDtos = tenants.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt,
                IsActive = t.IsActive,
                MemberCount = membersByTenant.ContainsKey(t.Id) ? membersByTenant[t.Id].Count : 0,
                Members = membersByTenant.ContainsKey(t.Id)
                    ? membersByTenant[t.Id].Select(m => new TenantMemberDto
                    {
                        Id = m.Id,
                        TenantId = m.TenantId,
                        CorporateKey = m.CorporateKey,
                        AddedBy = m.AddedBy,
                        AddedAt = m.AddedAt,
                        IsBlocked = m.IsBlocked,
                        Access = m.Access.ToString()
                    }).ToList()
                    : new List<TenantMemberDto>()
            }).OrderBy(t => t.Name).ToList();

            _logger.LogInformation("Found {Count} tenants for user", tenantDtos.Count);

            return Result<List<TeamDto>>.Success(tenantDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants for user: {CorporateKey}", request.CurrentUserCorporateKey);
            return Result<List<TeamDto>>.Failure($"Error getting tenants: {ex.Message}");
        }
    }
}
