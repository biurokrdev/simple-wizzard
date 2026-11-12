using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Application.Features.Tenants.Commands.RemoveTenantMember;

public class RemoveTenantMemberCommandHandler : IRequestHandler<RemoveTenantMemberCommand, Result<bool>>
{
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly IRepository<TenantMember, Guid> _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveTenantMemberCommandHandler> _logger;

    public RemoveTenantMemberCommandHandler(
        IRepository<Tenant, Guid> tenantRepository,
        IRepository<TenantMember, Guid> memberRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveTenantMemberCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result<bool>.Failure("Tenant not found");

            var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken);
            if (member == null)
                return Result<bool>.Failure("Member not found");

            // Allow creator to remove any member OR member to remove themselves
            var isCreator = tenant.CreatedBy == request.RequesterId;
            var isSelf = member.CorporateKey == request.RequesterCorporateKey;

            if (!isCreator && !isSelf)
                return Result<bool>.Failure("You can only remove yourself or you must be the tenant creator");

            await _memberRepository.DeleteAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed member {MemberId} from tenant {TenantId}", request.MemberId, request.TenantId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from tenant");
            return Result<bool>.Failure($"Error removing member: {ex.Message}");
        }
    }
}
