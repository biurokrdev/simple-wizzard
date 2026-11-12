using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Infrastructure.Persistence.Repositories;
using D2ApiCreator.Tests.Integration.Common;
using FluentAssertions;

namespace D2ApiCreator.Tests.Integration.Repositories;

[Collection("Database")]
public class ProjectVersionRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ProjectVersionRepository _repository;
    private Tenant _testTenant = null!;
    private User _testUser = null!;
    private Project _testProject = null!;

    public ProjectVersionRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        var context = _fixture.CreateDbContext();
        _repository = new ProjectVersionRepository(context);
    }

    public async Task InitializeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        _testTenant = new Tenant(Guid.NewGuid(), "Test Tenant");
        _testUser = new User(Guid.NewGuid(), _testTenant.Id, "testuser");
        _testProject = new Project(Guid.NewGuid(), _testTenant.Id, "TEST-VER", "Version Test Project", createdBy: null);
        
        context.Tenants.Add(_testTenant);
        context.Users.Add(_testUser);
        context.Projects.Add(_testProject);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        context.Projects.Remove(_testProject);
        context.Users.Remove(_testUser);
        context.Tenants.Remove(_testTenant);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetActiveVersionAsync_WhenActiveVersionExists_ShouldReturnIt()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version1 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        var version2 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 1);
        version2.Activate();
        
        context.ProjectVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveVersionAsync(_testProject.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(version2.Id);
        result.IsActive.Should().BeTrue();
        result.VersionTag.Should().Be("1.1");

        // Cleanup
        context.ProjectVersions.RemoveRange(version1, version2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetActiveVersionAsync_WhenNoActiveVersion_ShouldReturnNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        context.ProjectVersions.Add(version);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveVersionAsync(_testProject.Id);

        // Assert
        result.Should().BeNull();

        // Cleanup
        context.ProjectVersions.Remove(version);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetVersionHistoryAsync_ShouldReturnVersionsOrderedByMajorMinorDesc()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version1_0 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        var version1_1 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 1);
        var version2_0 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 2, 0);
        var version1_2 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 2);
        
        context.ProjectVersions.AddRange(version1_0, version1_1, version2_0, version1_2);
        await context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetVersionHistoryAsync(_testProject.Id)).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].VersionTag.Should().Be("2.0");
        result[1].VersionTag.Should().Be("1.2");
        result[2].VersionTag.Should().Be("1.1");
        result[3].VersionTag.Should().Be("1.0");

        // Cleanup
        context.ProjectVersions.RemoveRange(version1_0, version1_1, version2_0, version1_2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetWithDetailsAsync_ShouldIncludeFilesMapplngsAndApprovals()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0, _testUser.Id);
        var file = new FileEntity(Guid.NewGuid(), version.Id, "test.docx", new byte[] { 1, 2, 3 });
        var mapping = new Mapping(Guid.NewGuid(), version.Id, "{{tag}}", MappingType.Static, 
            System.Text.Json.JsonDocument.Parse("{\"value\":\"test\"}"), 1, _testUser.Id);
        var approval = new Approval(Guid.NewGuid(), version.Id, _testUser.Id);
        
        context.ProjectVersions.Add(version);
        context.Files.Add(file);
        context.Mappings.Add(mapping);
        context.Approvals.Add(approval);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithDetailsAsync(version.Id);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);
        result.Files.First().Filename.Should().Be("test.docx");
        result.Mappings.Should().HaveCount(1);
        result.Mappings.First().TagName.Should().Be("{{tag}}");
        result.Approvals.Should().HaveCount(1);

        // Cleanup
        context.Approvals.Remove(approval);
        context.Mappings.Remove(mapping);
        context.Files.Remove(file);
        context.ProjectVersions.Remove(version);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_ShouldReturnHighestVersion()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version1_0 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        var version2_5 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 2, 5);
        var version2_3 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 2, 3);
        
        context.ProjectVersions.AddRange(version1_0, version2_5, version2_3);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestVersionNumberAsync(_testProject.Id);

        // Assert
        result.Should().NotBeNull();
        result.Value.Major.Should().Be(2);
        result.Value.Minor.Should().Be(5);

        // Cleanup
        context.ProjectVersions.RemoveRange(version1_0, version2_5, version2_3);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_WhenNoVersions_ShouldReturnNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var emptyProject = new Project(Guid.NewGuid(), _testTenant.Id, "EMPTY", "Empty Project", createdBy: null);
        context.Projects.Add(emptyProject);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestVersionNumberAsync(emptyProject.Id);

        // Assert
        result.Should().BeNull();

        // Cleanup
        context.Projects.Remove(emptyProject);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task VersionTagExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var version = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        context.ProjectVersions.Add(version);
        await context.SaveChangesAsync();

        // Act
        var result = await _repository.VersionTagExistsAsync(_testProject.Id, "1.0");

        // Assert
        result.Should().BeTrue();

        // Cleanup
        context.ProjectVersions.Remove(version);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task VersionTagExistsAsync_WhenDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.VersionTagExistsAsync(_testProject.Id, "99.99");

        // Assert
        result.Should().BeFalse();
    }
}

