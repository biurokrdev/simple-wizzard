using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Tenants;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using MediatR;

namespace D2ApiCreator.Application.Features.Tenants.Admin;

/// <summary>Wszystkie zespoły z członkami (panel administracyjny, BR-018) — bez filtra członkostwa.</summary>
public record GetAllTenantsQuery : IRequest<Result<List<TeamDto>>>;

/// <summary>Administrator tworzy zespół i od razu przypisuje użytkowników (sam nie staje się członkiem).</summary>
public record AdminCreateTenantCommand(string Name, string? Description, IReadOnlyList<string> MemberCorporateKeys, Guid CreatedBy) : IRequest<Result<TeamDto>>;

/// <summary>Blokada / odblokowanie zespołu (`tenants.is_active`).</summary>
public record SetTenantActiveCommand(Guid TenantId, bool IsActive) : IRequest<Result<TeamDto>>;

/// <summary>Dodanie użytkownika do zespołu przez administratora.</summary>
public record AdminAddTenantMemberCommand(Guid TenantId, string CorporateKey, TenantMemberAccess Access, Guid AddedBy) : IRequest<Result<TeamDto>>;

/// <summary>Blokada użytkownika w zespole i/lub poziom dostępu (ReadWrite | ReadOnly).</summary>
public record UpdateTenantMemberCommand(Guid TenantId, Guid MemberId, bool? IsBlocked, TenantMemberAccess? Access) : IRequest<Result<TeamDto>>;

/// <summary>Usunięcie użytkownika z zespołu przez administratora.</summary>
public record AdminRemoveTenantMemberCommand(Guid TenantId, Guid MemberId) : IRequest<Result<TeamDto>>;

public static class TeamDtoMapper
{
    public static TeamDto ToDto(Tenant tenant, IEnumerable<TenantMember> members)
    {
        var list = members.OrderBy(m => m.CorporateKey).ToList();
        return new TeamDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Description = tenant.Description,
            CreatedBy = tenant.CreatedBy,
            CreatedAt = tenant.CreatedAt,
            IsActive = tenant.IsActive,
            MemberCount = list.Count,
            Members = list.Select(m => new TenantMemberDto
            {
                Id = m.Id,
                TenantId = m.TenantId,
                CorporateKey = m.CorporateKey,
                AddedBy = m.AddedBy,
                AddedAt = m.AddedAt,
                IsBlocked = m.IsBlocked,
                Access = m.Access.ToString()
            }).ToList()
        };
    }
}

public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, Result<List<TeamDto>>>
{
    private readonly IRepository<Tenant, Guid> _tenants;
    private readonly IRepository<TenantMember, Guid> _members;

    public GetAllTenantsQueryHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members)
    {
        _tenants = tenants;
        _members = members;
    }

    public async Task<Result<List<TeamDto>>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _tenants.GetAllAsync(cancellationToken);
        var members = (await _members.GetAllAsync(cancellationToken)).GroupBy(m => m.TenantId).ToDictionary(g => g.Key, g => g.ToList());
        var dtos = tenants
            .OrderBy(t => t.Name)
            .Select(t => TeamDtoMapper.ToDto(t, members.TryGetValue(t.Id, out var list) ? list : new List<TenantMember>()))
            .ToList();
        return Result<List<TeamDto>>.Success(dtos);
    }
}

/// <summary>Wspólna baza: wczytaj zespół z członkami, zapisz, zwróć DTO.</summary>
public abstract class TenantAdminHandlerBase
{
    protected readonly IRepository<Tenant, Guid> Tenants;
    protected readonly IRepository<TenantMember, Guid> Members;
    protected readonly IUnitOfWork UnitOfWork;

    protected TenantAdminHandlerBase(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
    {
        Tenants = tenants;
        Members = members;
        UnitOfWork = unitOfWork;
    }

    protected async Task<Result<TeamDto>> SaveAndMap(Tenant tenant, CancellationToken cancellationToken)
    {
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        var members = await Members.FindAsync(m => m.TenantId == tenant.Id, cancellationToken);
        return Result<TeamDto>.Success(TeamDtoMapper.ToDto(tenant, members));
    }
}

public class AdminCreateTenantCommandHandler : TenantAdminHandlerBase, IRequestHandler<AdminCreateTenantCommand, Result<TeamDto>>
{
    public AdminCreateTenantCommandHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
        : base(tenants, members, unitOfWork) { }

    public async Task<Result<TeamDto>> Handle(AdminCreateTenantCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return Result<TeamDto>.Failure("Tenant name is required");
        }
        if ((await Tenants.FindAsync(t => t.Name == name, cancellationToken)).Any())
        {
            return Result<TeamDto>.Failure($"Tenant '{name}' already exists");
        }

        var tenant = new Tenant(Guid.NewGuid(), name, request.CreatedBy);
        tenant.UpdateDescription(string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
        await Tenants.AddAsync(tenant, cancellationToken);

        foreach (var key in request.MemberCorporateKeys.Select(k => k.Trim()).Where(k => k.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await Members.AddAsync(new TenantMember(Guid.NewGuid(), tenant.Id, key, request.CreatedBy), cancellationToken);
        }

        return await SaveAndMap(tenant, cancellationToken);
    }
}

public class SetTenantActiveCommandHandler : TenantAdminHandlerBase, IRequestHandler<SetTenantActiveCommand, Result<TeamDto>>
{
    public SetTenantActiveCommandHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
        : base(tenants, members, unitOfWork) { }

    public async Task<Result<TeamDto>> Handle(SetTenantActiveCommand request, CancellationToken cancellationToken)
    {
        var tenant = await Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TeamDto>.Failure($"Tenant {request.TenantId} not found");
        }
        if (request.IsActive) tenant.Activate(); else tenant.Deactivate();
        // encja śledzona — SaveChanges zapisze tylko zmienione kolumny (Update() oznaczałby też `created_at` bez strefy, R-08)
        return await SaveAndMap(tenant, cancellationToken);
    }
}

public class AdminAddTenantMemberCommandHandler : TenantAdminHandlerBase, IRequestHandler<AdminAddTenantMemberCommand, Result<TeamDto>>
{
    public AdminAddTenantMemberCommandHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
        : base(tenants, members, unitOfWork) { }

    public async Task<Result<TeamDto>> Handle(AdminAddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenant = await Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TeamDto>.Failure($"Tenant {request.TenantId} not found");
        }
        var key = request.CorporateKey.Trim();
        if (key.Length == 0)
        {
            return Result<TeamDto>.Failure("Corporate key is required");
        }
        if ((await Members.FindAsync(m => m.TenantId == tenant.Id && m.CorporateKey == key, cancellationToken)).Any())
        {
            return Result<TeamDto>.Failure($"User '{key}' is already a member of this tenant");
        }

        var member = new TenantMember(Guid.NewGuid(), tenant.Id, key, request.AddedBy);
        member.SetAccess(request.Access);
        await Members.AddAsync(member, cancellationToken);
        return await SaveAndMap(tenant, cancellationToken);
    }
}

public class UpdateTenantMemberCommandHandler : TenantAdminHandlerBase, IRequestHandler<UpdateTenantMemberCommand, Result<TeamDto>>
{
    public UpdateTenantMemberCommandHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
        : base(tenants, members, unitOfWork) { }

    public async Task<Result<TeamDto>> Handle(UpdateTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenant = await Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TeamDto>.Failure($"Tenant {request.TenantId} not found");
        }
        var member = await Members.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null || member.TenantId != tenant.Id)
        {
            return Result<TeamDto>.Failure($"Member {request.MemberId} not found in tenant");
        }

        if (request.IsBlocked is { } blocked)
        {
            if (blocked) member.Block(); else member.Unblock();
        }
        if (request.Access is { } access)
        {
            member.SetAccess(access);
        }
        // encja śledzona — bez Update(): `added_at` to timestamp bez strefy (R-08), Update() próbowałby go zapisać ponownie
        return await SaveAndMap(tenant, cancellationToken);
    }
}

public class AdminRemoveTenantMemberCommandHandler : TenantAdminHandlerBase, IRequestHandler<AdminRemoveTenantMemberCommand, Result<TeamDto>>
{
    public AdminRemoveTenantMemberCommandHandler(IRepository<Tenant, Guid> tenants, IRepository<TenantMember, Guid> members, IUnitOfWork unitOfWork)
        : base(tenants, members, unitOfWork) { }

    public async Task<Result<TeamDto>> Handle(AdminRemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenant = await Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TeamDto>.Failure($"Tenant {request.TenantId} not found");
        }
        var member = await Members.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null || member.TenantId != tenant.Id)
        {
            return Result<TeamDto>.Failure($"Member {request.MemberId} not found in tenant");
        }
        await Members.DeleteAsync(member, cancellationToken);
        return await SaveAndMap(tenant, cancellationToken);
    }
}
