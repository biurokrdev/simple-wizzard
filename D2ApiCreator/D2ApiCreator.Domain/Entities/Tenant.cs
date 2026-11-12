using System.Text.Json;

namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;

public class Tenant : AggregateRoot<Guid>
{
    private readonly List<User> _users = new();
    private readonly List<Project> _projects = new();
    private readonly List<TenantMember> _members = new();

    protected Tenant() { }

    public Tenant(Guid id, string name)
        : base(id)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Tenant(Guid id, string name, Guid createdBy)
        : base(id)
    {
        Name = name;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public JsonDocument? Metadata { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();
    public IReadOnlyCollection<TenantMember> Members => _members.AsReadOnly();

    public void UpdateMetadata(JsonDocument? metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        IsActive = true;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }

    public void AddMember(TenantMember member)
    {
        if (!_members.Any(m => m.CorporateKey == member.CorporateKey))
        {
            _members.Add(member);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveMember(Guid memberId)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId);
        if (member != null)
        {
            _members.Remove(member);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}

