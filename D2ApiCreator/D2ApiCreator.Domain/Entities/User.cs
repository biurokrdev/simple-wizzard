namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;

public class User : AggregateRoot<Guid>
{
    protected User() { }

    public User(Guid id, Guid tenantId, string username, string? email = null, string? displayName = null)
        : base(id)
    {
        TenantId = tenantId;
        Username = username;
        Email = email;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public string Username { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public Tenant Tenant { get; private set; } = null!;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

