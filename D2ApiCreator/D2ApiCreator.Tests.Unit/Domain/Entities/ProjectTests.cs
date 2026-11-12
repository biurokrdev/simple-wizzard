using D2ApiCreator.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace D2ApiCreator.Tests.Unit.Domain.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultRetentionDays()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var source = "TEST-001";
        var name = "Test Project";

        // Act
        var project = new Project(id, tenantId, source, name);

        // Assert
        project.Id.Should().Be(id);
        project.TenantId.Should().Be(tenantId);
        project.Source.Should().Be(source);
        project.Name.Should().Be(name);
        project.RetentionDays.Should().Be(90);
        project.CurrentActiveVersionId.Should().BeNull();
    }

    [Fact]
    public void SetActiveVersion_ShouldUpdateCurrentActiveVersionId()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), Guid.NewGuid(), "TEST-001", "Test");
        var versionId = Guid.NewGuid();

        // Act
        project.SetActiveVersion(versionId);

        // Assert
        project.CurrentActiveVersionId.Should().Be(versionId);
    }

    [Fact]
    public void ClearActiveVersion_ShouldResetCurrentActiveVersionIdAndTouchUpdatedAt()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), Guid.NewGuid(), "TEST-001", "Test");
        project.SetActiveVersion(Guid.NewGuid());

        // Act
        project.ClearActiveVersion();

        // Assert
        project.CurrentActiveVersionId.Should().BeNull();
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDescription_ShouldSetDescription()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), Guid.NewGuid(), "TEST-001", "Test");
        var description = "New description";

        // Act
        project.UpdateDescription(description);

        // Assert
        project.Description.Should().Be(description);
    }

    [Fact]
    public void UpdateTemplateName_ShouldSetTemplateName()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), Guid.NewGuid(), "TEST-001", "Test");
        var templateName = "template.docx";

        // Act
        project.UpdateTemplateName(templateName);

        // Assert
        project.DefaultTemplateName.Should().Be(templateName);
    }
}

