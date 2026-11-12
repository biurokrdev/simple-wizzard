using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Tenants.Commands.AddTenantMember;

public class AddTenantMemberCommandHandler : IRequestHandler<AddTenantMemberCommand, Result<bool>>
{
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IRepository<TenantMember, Guid> _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddTenantMemberCommandHandler> _logger;

    public AddTenantMemberCommandHandler(
        IRepository<Tenant, Guid> tenantRepository,
        IRepository<TenantMember, Guid> memberRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddTenantMemberCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result<bool>.Failure("Tenant not found");

            // Check if requester is the creator
            if (tenant.CreatedBy != request.AddedBy)
                return Result<bool>.Failure("Only tenant creator can add members");

            // Check if member already exists
            var existingMember = await _memberRepository.FindAsync(
                m => m.TenantId == request.TenantId && m.CorporateKey == request.CorporateKey,
                cancellationToken);

            if (existingMember.Any())
                return Result<bool>.Failure("Member already exists in this tenant");

            var member = new TenantMember(
                Guid.NewGuid(),
                request.TenantId,
                request.CorporateKey,
                request.AddedBy
            );

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added member {CorporateKey} to tenant {TenantId}", request.CorporateKey, request.TenantId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to tenant");
            return Result<bool>.Failure($"Error adding member: {ex.Message}");
        }
    }
}
