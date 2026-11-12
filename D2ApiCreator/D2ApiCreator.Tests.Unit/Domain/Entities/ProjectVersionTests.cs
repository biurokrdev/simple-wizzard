using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace D2ApiCreator.Tests.Unit.Domain.Entities;

public class ProjectVersionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var version = new ProjectVersion(id, projectId, 1, 0, createdBy);

        // Assert
        version.Id.Should().Be(id);
        version.ProjectId.Should().Be(projectId);
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
        version.VersionTag.Should().Be("1.0");
        version.IsActive.Should().BeFalse();
        version.Status.Should().Be(ProjectStatus.Draft);
        version.CreatedBy.Should().Be(createdBy);
        version.StepData.Should().NotBeNull();
        version.StepData!.RootElement.GetRawText().Should().Be("{}");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndUpdateStatus()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());

        // Act
        version.Activate();

        // Assert
        version.IsActive.Should().BeTrue();
        version.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Activate_WhenStatusIsApproved_ShouldSetToActive()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());
        version.UpdateStatus(ProjectStatus.Approved);

        // Act
        version.Activate();

        // Assert
        version.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndStatusToInactive()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());
        version.Activate();

        // Act
        version.Deactivate();

        // Assert
        version.IsActive.Should().BeFalse();
        version.Status.Should().Be(ProjectStatus.Inactive);
    }

    [Fact]
    public void UpdateStatus_ShouldChangeStatus()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());

        // Act
        version.UpdateStatus(ProjectStatus.PendingApproval);

        // Assert
        version.Status.Should().Be(ProjectStatus.PendingApproval);
    }

    [Fact]
    public void UpdateStepData_ShouldReplaceStepData()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());
        var newData = JsonDocument.Parse("{\"step1\": {\"name\": \"Test\"}}");

        // Act
        version.UpdateStepData(newData);

        // Assert
        version.StepData.Should().NotBeNull();
        version.StepData!.RootElement.GetProperty("step1").GetProperty("name").GetString().Should().Be("Test");
    }

    [Fact]
    public void UpdateNotes_ShouldSetNotes()
    {
        // Arrange
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());
        var notes = "Test notes";

        // Act
        version.UpdateNotes(notes);

        // Assert
        version.Notes.Should().Be(notes);
    }

    [Theory]
    [InlineData(2, 0, "2.0")]
    [InlineData(1, 5, "1.5")]
    [InlineData(10, 15, "10.15")]
    public void Constructor_ShouldGenerateCorrectVersionTag(int major, int minor, string expectedTag)
    {
        // Arrange & Act
        var version = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid(), major, minor);

        // Assert
        version.VersionTag.Should().Be(expectedTag);
    }
}

