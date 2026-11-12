using D2ApiCreator.Application.Common;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Commands.RemoveTenantMember;

public class RemoveTenantMemberCommand : IRequest<Result<bool>>
{
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterCorporateKey { get; set; } = string.Empty;
}
