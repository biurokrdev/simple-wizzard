using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Infrastructure.Persistence.Repositories;
using D2ApiCreator.Tests.Integration.Common;
using FluentAssertions;

namespace D2ApiCreator.Tests.Integration.Repositories;

[Collection("Database")]
public class ProjectRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ProjectRepository _repository;
    private Tenant _testTenant = null!;
    private User _testUser = null!;

    public ProjectRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        var context = _fixture.CreateDbContext();
        _repository = new ProjectRepository(context);
    }

    public async Task InitializeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        // Create test tenant and user
        _testTenant = new Tenant(Guid.NewGuid(), "Test Tenant");
        _testUser = new User(Guid.NewGuid(), _testTenant.Id, "testuser");
        
        context.Tenants.Add(_testTenant);
        context.Users.Add(_testUser);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        // Clean up test data
        context.Users.RemoveRange(context.Users.Where(u => u.TenantId == _testTenant.Id));
        context.Tenants.Remove(_testTenant);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetBySourceAsync_WhenProjectExists_ShouldReturnProject()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var project = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-001", "Test Project", createdBy: _testUser.Id);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySourceAsync(_testTenant.Id, "TEST-001");

        // Assert
        result.Should().NotBeNull();
        result.Source.Should().Be("TEST-001");
        result.Name.Should().Be("Test Project");

        // Cleanup
        context.Projects.Remove(project);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetBySourceAsync_WhenProjectDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetBySourceAsync(_testTenant.Id, "NON-EXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySourceAsync_WhenDifferentTenant_ShouldReturnNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var project = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-002", "Test Project", createdBy: null);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var differentTenantId = Guid.NewGuid();

        // Act
        var result = await _repository.GetBySourceAsync(differentTenantId, "TEST-002");

        // Assert
        result.Should().BeNull();

        // Cleanup
        context.Projects.Remove(project);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetWithVersionsAsync_ShouldIncludeAllVersions()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var project = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-003", "Test Project", createdBy: null);
        var version1 = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 0, _testUser.Id);
        var version2 = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 1, _testUser.Id);
        
        context.Projects.Add(project);
        context.ProjectVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithVersionsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result.Versions.Should().HaveCount(2);
        result.Versions.Should().Contain(v => v.VersionTag == "1.0");
        result.Versions.Should().Contain(v => v.VersionTag == "1.1");

        // Cleanup
        context.ProjectVersions.RemoveRange(version1, version2);
        context.Projects.Remove(project);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetWithActiveVersionAsync_ShouldOnlyIncludeActiveVersion()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var project = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-004", "Test Project", createdBy: null);
        var version1 = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 0);
        var version2 = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 1);
        version2.Activate();
        
        context.Projects.Add(project);
        context.ProjectVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithActiveVersionAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result.Versions.Should().HaveCount(1);
        result.Versions.First().VersionTag.Should().Be("1.1");
        result.Versions.First().IsActive.Should().BeTrue();

        // Cleanup
        context.ProjectVersions.RemoveRange(version1, version2);
        context.Projects.Remove(project);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllForTenantAsync_ShouldReturnProjectsForTenantOnly()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var project1 = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-005", "Project 1", createdBy: null);
        var project2 = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-006", "Project 2", createdBy: null);
        
        var otherTenant = new Tenant(Guid.NewGuid(), "Other Tenant");
        var otherProject = new Project(Guid.NewGuid(), otherTenant.Id, "OTHER-001", "Other Project", createdBy: null);
        
        context.Tenants.Add(otherTenant);
        context.Projects.AddRange(project1, project2, otherProject);
        await context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllForTenantAsync(_testTenant.Id)).ToList();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().AllSatisfy(p => p.TenantId.Should().Be(_testTenant.Id));
        result.Should().Contain(p => p.Source == "TEST-005");
        result.Should().Contain(p => p.Source == "TEST-006");
        result.Should().NotContain(p => p.Source == "OTHER-001");

        // Cleanup
        context.Projects.RemoveRange(project1, project2, otherProject);
        context.Tenants.Remove(otherTenant);
        await context.SaveChangesAsync();
    }
}

