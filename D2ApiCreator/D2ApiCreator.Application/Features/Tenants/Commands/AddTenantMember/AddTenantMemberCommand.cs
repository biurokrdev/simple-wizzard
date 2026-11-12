using D2ApiCreator.Application.Common;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Commands.AddTenantMember;

public class AddTenantMemberCommand : IRequest<Result<bool>>
{
    public Guid TenantId { get; set; }
    public string CorporateKey { get; set; } = string.Empty;
    public Guid AddedBy { get; set; }
    public string RequesterCorporateKey { get; set; } = string.Empty;
}
