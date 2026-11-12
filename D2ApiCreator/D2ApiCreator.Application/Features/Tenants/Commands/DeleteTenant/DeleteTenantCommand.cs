using D2ApiCreator.Application.Common;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommand : IRequest<Result<bool>>
{
    public Guid TenantId { get; set; }
    public Guid RequesterId { get; set; }
}
