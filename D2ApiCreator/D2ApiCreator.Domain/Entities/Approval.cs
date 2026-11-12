namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;
using Enums;

public class Approval : AggregateRoot<Guid>
{
    protected Approval() { }

    public Approval(Guid id, Guid projectVersionId, Guid? approverId = null)
        : base(id)
    {
        ProjectVersionId = projectVersionId;
        ApproverId = approverId;
        Status = ApprovalStatus.Requested;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid ProjectVersionId { get; private set; }
    public Guid? ApproverId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public ProjectVersion ProjectVersion { get; private set; } = null!;
    public User? Approver { get; private set; }

    public void Approve(string? comment = null)
    {
        Status = ApprovalStatus.Approved;
        Comment = comment;
    }

    public void Reject(string? comment = null)
    {
        Status = ApprovalStatus.Rejected;
        Comment = comment;
    }
}

