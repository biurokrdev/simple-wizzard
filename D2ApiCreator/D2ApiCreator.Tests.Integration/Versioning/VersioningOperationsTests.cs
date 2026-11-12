using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Tests.Integration.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace D2ApiCreator.Tests.Integration.Versioning;

[Collection("Database")]
public class VersioningOperationsTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Tenant _testTenant = null!;
    private User _testUser = null!;
    private Project _testProject = null!;

    public VersioningOperationsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        _testTenant = new Tenant(Guid.NewGuid(), "Test Tenant");
        _testUser = new User(Guid.NewGuid(), _testTenant.Id, "testuser");
        _testProject = new Project(Guid.NewGuid(), _testTenant.Id, "VER-TEST", "Versioning Test", createdBy: null);
        
        context.Tenants.Add(_testTenant);
        context.Users.Add(_testUser);
        context.Projects.Add(_testProject);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        
        // Clean up in reverse order of dependencies
        var versions = await context.ProjectVersions.Where(v => v.ProjectId == _testProject.Id).ToListAsync();
        foreach (var version in versions)
        {
            var files = await context.Files.Where(f => f.ProjectVersionId == version.Id).ToListAsync();
            var mappings = await context.Mappings.Where(m => m.ProjectVersionId == version.Id).ToListAsync();
            var approvals = await context.Approvals.Where(a => a.ProjectVersionId == version.Id).ToListAsync();
            
            context.Files.RemoveRange(files);
            context.Mappings.RemoveRange(mappings);
            context.Approvals.RemoveRange(approvals);
        }
        context.ProjectVersions.RemoveRange(versions);
        context.Projects.Remove(_testProject);
        context.Users.Remove(_testUser);
        context.Tenants.Remove(_testTenant);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CopyOnWrite_ShouldCreateNewVersionWithCopiedData()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        
        // Create source version with file and mapping
        var sourceVersion = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0, _testUser.Id);
        sourceVersion.UpdateStepData(JsonDocument.Parse("{\"step1\": {\"name\": \"Test\"}}"));
        
        var sourceFile = new FileEntity(Guid.NewGuid(), sourceVersion.Id, "template.docx", 
            new byte[] { 1, 2, 3, 4, 5 }, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 5, "abc123");
        
        var sourceMapping = new Mapping(Guid.NewGuid(), sourceVersion.Id, "{{CompanyName}}", 
            MappingType.JsonPath, JsonDocument.Parse("{\"path\": \"$.company.name\"}"), 1, _testUser.Id);
        
        context.ProjectVersions.Add(sourceVersion);
        context.Files.Add(sourceFile);
        context.Mappings.Add(sourceMapping);
        await context.SaveChangesAsync();

        // Act - Create new minor version (copy-on-write)
        var newVersion = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 
            sourceVersion.Major, sourceVersion.Minor + 1, _testUser.Id);
        newVersion.UpdateStepData(sourceVersion.StepData!);
        
        // Copy file
        var newFile = new FileEntity(Guid.NewGuid(), newVersion.Id, sourceFile.Filename, 
            sourceFile.Content!, sourceFile.ContentType, sourceFile.Size, sourceFile.Sha256);
        
        // Copy mapping
        var newMapping = new Mapping(Guid.NewGuid(), newVersion.Id, sourceMapping.TagName, 
            sourceMapping.MappingType, sourceMapping.MappingJson, sourceMapping.SortOrder, _testUser.Id);
        
        context.ProjectVersions.Add(newVersion);
        context.Files.Add(newFile);
        context.Mappings.Add(newMapping);
        await context.SaveChangesAsync();

        // Assert
        var versions = await context.ProjectVersions
            .Where(v => v.ProjectId == _testProject.Id)
            .OrderBy(v => v.Minor)
            .ToListAsync();
        
        versions.Should().HaveCount(2);
        versions[0].VersionTag.Should().Be("1.0");
        versions[1].VersionTag.Should().Be("1.1");
        
        // Verify copied data
        var copiedFiles = await context.Files.Where(f => f.ProjectVersionId == newVersion.Id).ToListAsync();
        copiedFiles.Should().HaveCount(1);
        copiedFiles[0].Filename.Should().Be("template.docx");
        copiedFiles[0].Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
        
        var copiedMappings = await context.Mappings.Where(m => m.ProjectVersionId == newVersion.Id).ToListAsync();
        copiedMappings.Should().HaveCount(1);
        copiedMappings[0].TagName.Should().Be("{{CompanyName}}");
        
        newVersion.StepData!.RootElement.GetProperty("step1").GetProperty("name").GetString().Should().Be("Test");
    }

    [Fact]
    public async Task SetActiveVersion_ShouldDeactivateOthersAndActivateSelected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        
        var version1 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        var version2 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 1);
        var version3 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 2, 0);
        
        version1.Activate(); // Initially active
        
        context.ProjectVersions.AddRange(version1, version2, version3);
        await context.SaveChangesAsync();

        // Act - Set version2 as active
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            // Deactivate all other versions
            var otherVersions = await context.ProjectVersions
                .Where(v => v.ProjectId == _testProject.Id && v.Id != version2.Id && v.IsActive)
                .ToListAsync();
            
            foreach (var v in otherVersions)
            {
                v.Deactivate();
            }
            
            // Activate selected version
            version2.Activate();
            _testProject.SetActiveVersion(version2.Id);
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Assert
        await using (var verifyContext = _fixture.CreateDbContext())
        {
            var allVersions = await verifyContext.ProjectVersions
                .Where(v => v.ProjectId == _testProject.Id)
                .ToListAsync();
            
            var activeVersions = allVersions.Where(v => v.IsActive).ToList();
            activeVersions.Should().HaveCount(1);
            activeVersions[0].Id.Should().Be(version2.Id);
            activeVersions[0].Status.Should().Be(ProjectStatus.Active);
            
            var project = await verifyContext.Projects.FindAsync(_testProject.Id);
            project!.CurrentActiveVersionId.Should().Be(version2.Id);
        }
    }

    [Fact]
    public async Task SetActiveVersion_WithTrigger_ShouldEnforceOnlyOneActive()
    {
        // This test verifies that the partial unique index prevents multiple active versions
        // Note: This requires the actual trigger/index to be set up in the database
        
        // Arrange
        await using var context = _fixture.CreateDbContext();
        
        var version1 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0);
        var version2 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 1);
        
        context.ProjectVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        // Act
        version1.Activate();
        await context.SaveChangesAsync();
        
        version2.Activate();
        await context.SaveChangesAsync();

        // Assert - verify only one is active (trigger should have handled this)
        await using (var verifyContext = _fixture.CreateDbContext())
        {
            var activeCount = await verifyContext.ProjectVersions
                .CountAsync(v => v.ProjectId == _testProject.Id && v.IsActive);
            
            // If trigger works correctly, this should be 1
            // If not, manual deactivation in application code is needed
            activeCount.Should().BeLessThanOrEqualTo(1, "Only one version should be active at a time");
        }
    }

    [Fact]
    public async Task CreateMajorVersion_ShouldIncrementMajorAndResetMinor()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        
        var currentVersion = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 5, _testUser.Id);
        context.ProjectVersions.Add(currentVersion);
        await context.SaveChangesAsync();

        // Act - Create new major version
        var newMajorVersion = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 
            currentVersion.Major + 1, 0, _testUser.Id);
        
        context.ProjectVersions.Add(newMajorVersion);
        await context.SaveChangesAsync();

        // Assert
        newMajorVersion.VersionTag.Should().Be("2.0");
        newMajorVersion.Major.Should().Be(2);
        newMajorVersion.Minor.Should().Be(0);
    }

    [Fact]
    public async Task VersionHistory_ShouldMaintainCompleteAuditTrail()
    {
        // Arrange & Act
        await using var context = _fixture.CreateDbContext();
        
        var v1_0 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 0, _testUser.Id);
        v1_0.UpdateNotes("Initial version");
        v1_0.Activate();
        
        var v1_1 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 1, 1, _testUser.Id);
        v1_1.UpdateNotes("Minor update");
        
        var v2_0 = new ProjectVersion(Guid.NewGuid(), _testProject.Id, 2, 0, _testUser.Id);
        v2_0.UpdateNotes("Major release");
        v2_0.Activate();
        
        context.ProjectVersions.AddRange(v1_0, v1_1, v2_0);
        await context.SaveChangesAsync();

        // Assert
        var history = await context.ProjectVersions
            .Where(v => v.ProjectId == _testProject.Id)
            .Include(v => v.Creator)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
        
        history.Should().HaveCount(3);
        history.Should().AllSatisfy(v => v.CreatedBy.Should().Be(_testUser.Id));
        history.Should().AllSatisfy(v => v.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1)));
        
        var activeVersions = history.Where(v => v.IsActive).ToList();
        activeVersions.Should().HaveCount(1);
        activeVersions[0].VersionTag.Should().Be("2.0");
    }
}

