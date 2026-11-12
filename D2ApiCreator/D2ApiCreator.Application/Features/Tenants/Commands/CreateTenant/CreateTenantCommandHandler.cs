using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Tenants;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Handler for creating a new tenant
/// </summary>
public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TeamDto>>
{
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IRepository<TenantMember, Guid> _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        IRepository<Tenant, Guid> tenantRepository,
        IRepository<TenantMember, Guid> memberRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TeamDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating tenant: {Name} by user: {CreatedBy}", request.Name, request.CreatedByCorporateKey);

            // Create tenant
            var tenant = new Tenant(Guid.NewGuid(), request.Name, request.CreatedBy);
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                tenant.UpdateDescription(request.Description);
            }

            await _tenantRepository.AddAsync(tenant, cancellationToken);

            // Add creator as a member
            var creatorMember = new TenantMember(
                Guid.NewGuid(),
                tenant.Id,
                request.CreatedByCorporateKey,
                request.CreatedBy
            );
            await _memberRepository.AddAsync(creatorMember, cancellationToken);

            var members = new List<TenantMember> { creatorMember };

            // Add additional members
            foreach (var corporateKey in request.MemberCorporateKeys.Where(k => k != request.CreatedByCorporateKey))
            {
                var member = new TenantMember(
                    Guid.NewGuid(),
                    tenant.Id,
                    corporateKey,
                    request.CreatedBy
                );
                await _memberRepository.AddAsync(member, cancellationToken);
                members.Add(member);
            }

            // Save changes to database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tenantDto = new TeamDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Description = tenant.Description,
                CreatedBy = tenant.CreatedBy,
                CreatedAt = tenant.CreatedAt,
                MemberCount = members.Count,
                Members = members.Select(m => new TenantMemberDto
                {
                    Id = m.Id,
                    TenantId = m.TenantId,
                    CorporateKey = m.CorporateKey,
                    AddedBy = m.AddedBy,
                    AddedAt = m.AddedAt
                }).ToList()
            };

            _logger.LogInformation("Created tenant {TenantId} with {MemberCount} members", tenant.Id, members.Count);

            return Result<TeamDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant: {Name}", request.Name);
            return Result<TeamDto>.Failure($"Error creating tenant: {ex.Message}");
        }
    }
}
