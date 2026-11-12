namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;
using ValueObjects;

public class Project : AggregateRoot<Guid>
{
    private readonly List<ProjectVersion> _versions = new();

    protected Project() { }

    public Project(
        Guid id, 
        Guid? tenantId, 
        string source, 
        string name, 
        ProjectMetadata? metadata = null,
        int retentionDays = 90,
        Guid? createdBy = null)
        : base(id)
    {
        TenantId = tenantId;
        Source = source;
        Name = name;
        Metadata = metadata ?? new ProjectMetadata();
        RetentionDays = retentionDays;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid? TenantId { get; private set; }
    public string Source { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ProjectMetadata Metadata { get; private set; } = new();
    public int RetentionDays { get; private set; }
    public string? DefaultTemplateName { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? Description { get; private set; }
    public Guid? CurrentActiveVersionId { get; private set; }

    // Navigation properties
    public Tenant? Tenant { get; private set; }
    public User? Creator { get; private set; }
    public IReadOnlyCollection<ProjectVersion> Versions => _versions.AsReadOnly();

    public void SetActiveVersion(Guid versionId)
    {
        CurrentActiveVersionId = versionId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Leaves the project without an active version (e.g. after the active version is deactivated
    /// through the external Services API). No fallback to another version is applied on purpose.
    /// </summary>
    public void ClearActiveVersion()
    {
        CurrentActiveVersionId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTemplateName(string? templateName)
    {
        DefaultTemplateName = templateName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRetentionDays(int retentionDays)
    {
        if (retentionDays < 0)
            throw new ArgumentException("Retention days cannot be negative", nameof(retentionDays));
        RetentionDays = retentionDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(ProjectMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }
}

