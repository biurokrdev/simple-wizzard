

using System.Text.Json;

namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;
using Enums;

public class ProjectVersion : AggregateRoot<Guid>
{
    private readonly List<FileEntity> _files = new();
    private readonly List<Mapping> _mappings = new();
    private readonly List<Approval> _approvals = new();
    private readonly List<Worker> _workers = new();

    protected ProjectVersion() { }

    public ProjectVersion(Guid id, Guid projectId, int major = 1, int minor = 0, Guid? createdBy = null)
        : base(id)
    {
        ProjectId = projectId;
        Major = major;
        Minor = minor;
        VersionTag = $"{major}.{minor}";
        IsActive = false;
        Status = ProjectStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        StepData = JsonDocument.Parse("{}");
    }

    public Guid ProjectId { get; private set; }
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public string VersionTag { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public JsonDocument? StepData { get; private set; }
    public string? Notes { get; private set; }

    // Przegląd/akceptacja (BR-017)
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewComment { get; private set; }
    // Paczka wdrożeniowa (BR-022): ostatnia wysyłka do aplikacji docelowej
    public DateTime? DeliveredAt { get; private set; }
    public string? DeliveredTo { get; private set; }
    public string? DeliveredPackageSha { get; private set; }

    /// <summary>Uwagi recenzenta do poszczególnych kroków kreatora: { "documentDescription": "…", … } (JSONB).</summary>
    public JsonDocument? ReviewNotes { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User? Creator { get; private set; }
    public IReadOnlyCollection<FileEntity> Files => _files.AsReadOnly();
    public IReadOnlyCollection<Mapping> Mappings => _mappings.AsReadOnly();
    public IReadOnlyCollection<Approval> Approvals => _approvals.AsReadOnly();
    public IReadOnlyCollection<Worker> Workers => _workers.AsReadOnly();

    public void Activate()
    {
        IsActive = true;
        if (Status == ProjectStatus.Draft || Status == ProjectStatus.Approved)
        {
            Status = ProjectStatus.Active;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = ProjectStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(ProjectStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStepData(JsonDocument stepData)
    {
        StepData = stepData;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Projektant zgłasza wersję do publikacji (BR-017): Draft | RequiresChanges → PendingApproval.</summary>
    public void SubmitForReview()
    {
        Status = ProjectStatus.PendingApproval;
        SubmittedAt = DateTime.UtcNow;
        ReviewedAt = null;
        ReviewedBy = null;
        ReviewComment = null;
        ReviewNotes = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Paczka wersji została przyjęta przez aplikację docelową (BR-022).</summary>
    public void MarkDelivered(DateTime deliveredAt, string target, string packageSha256)
    {
        DeliveredAt = deliveredAt;
        DeliveredTo = target;
        DeliveredPackageSha = packageSha256;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Recenzent zapisuje uwagi do kroków (w trakcie przeglądu, przed decyzją).</summary>
    public void SetReviewNotes(JsonDocument? notes)
    {
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Czy są jakiekolwiek niepuste uwagi do kroków.</summary>
    public bool HasReviewNotes()
    {
        if (ReviewNotes is null || ReviewNotes.RootElement.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in ReviewNotes.RootElement.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.Value.GetString())) return true;
        }
        return false;
    }

    /// <summary>Administrator akceptuje: PendingApproval → Approved (aktywacja to osobna decyzja).</summary>
    public void ApproveReview(string? reviewer, string? comment)
    {
        Status = ProjectStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewer;
        ReviewComment = string.IsNullOrWhiteSpace(comment) ? null : comment;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Administrator odsyła do poprawy: PendingApproval → RequiresChanges („Wymaga poprawy”).</summary>
    public void RejectReview(string? reviewer, string comment)
    {
        Status = ProjectStatus.RequiresChanges;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewer;
        ReviewComment = comment;
        UpdatedAt = DateTime.UtcNow;
    }
}

