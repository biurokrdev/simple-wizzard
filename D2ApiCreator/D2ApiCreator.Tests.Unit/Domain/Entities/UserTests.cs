using D2ApiCreator.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace D2ApiCreator.Tests.Unit.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldInitializeAsActive()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var username = "testuser";

        // Act
        var user = new User(id, tenantId, username);

        // Assert
        user.Id.Should().Be(id);
        user.TenantId.Should().Be(tenantId);
        user.Username.Should().Be(username);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "testuser");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "testuser");
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }
}

