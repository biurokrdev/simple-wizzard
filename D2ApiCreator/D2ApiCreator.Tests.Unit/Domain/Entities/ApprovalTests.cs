using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace D2ApiCreator.Tests.Unit.Domain.Entities;

public class ApprovalTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithRequestedStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        // Act
        var approval = new Approval(id, versionId, approverId);

        // Assert
        approval.Id.Should().Be(id);
        approval.ProjectVersionId.Should().Be(versionId);
        approval.ApproverId.Should().Be(approverId);
        approval.Status.Should().Be(ApprovalStatus.Requested);
        approval.Comment.Should().BeNull();
    }

    [Fact]
    public void Approve_ShouldSetStatusToApproved()
    {
        // Arrange
        var approval = new Approval(Guid.NewGuid(), Guid.NewGuid());

        // Act
        approval.Approve();

        // Assert
        approval.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public void Approve_WithComment_ShouldSetStatusAndComment()
    {
        // Arrange
        var approval = new Approval(Guid.NewGuid(), Guid.NewGuid());
        var comment = "Looks good!";

        // Act
        approval.Approve(comment);

        // Assert
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.Comment.Should().Be(comment);
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        // Arrange
        var approval = new Approval(Guid.NewGuid(), Guid.NewGuid());

        // Act
        approval.Reject();

        // Assert
        approval.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Reject_WithComment_ShouldSetStatusAndComment()
    {
        // Arrange
        var approval = new Approval(Guid.NewGuid(), Guid.NewGuid());
        var comment = "Needs changes";

        // Act
        approval.Reject(comment);

        // Assert
        approval.Status.Should().Be(ApprovalStatus.Rejected);
        approval.Comment.Should().Be(comment);
    }
}

