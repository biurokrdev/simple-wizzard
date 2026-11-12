using D2ApiCreator.Domain.Common.BaseTypes;
using D2ApiCreator.Domain.Enums;

namespace D2ApiCreator.Domain.Entities;

/// <summary>
/// Represents a member of a tenant/team
/// </summary>
public class TenantMember : Entity<Guid>
{
    protected TenantMember() { }

    public TenantMember(Guid id, Guid tenantId, string corporateKey, Guid addedBy)
        : base(id)
    {
        TenantId = tenantId;
        CorporateKey = corporateKey;
        AddedBy = addedBy;
        AddedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reference to the tenant/team
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Corporate Key - user identifier in the system
    /// </summary>
    public string CorporateKey { get; set; } = string.Empty;

    /// <summary>
    /// User ID who added this member
    /// </summary>
    public Guid AddedBy { get; set; }

    /// <summary>
    /// When the member was added
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>Zablokowany: nie widzi projektów zespołu i nie może w nich pracować (BR-018).</summary>
    public bool IsBlocked { get; private set; }

    /// <summary>Poziom dostępu do projektów zespołu (BR-018).</summary>
    public TenantMemberAccess Access { get; private set; } = TenantMemberAccess.ReadWrite;

    public void Block() => IsBlocked = true;
    public void Unblock() => IsBlocked = false;
    public void SetAccess(TenantMemberAccess access) => Access = access;

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    public Tenant Tenant { get; set; } = null!;
}
