using System.Linq.Expressions;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Features.Projects.Commands.DeactivateProjectVersion;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Projects;

/// <summary>
/// Unit tests for DeactivateProjectVersionCommandHandler — used by the external Services API
/// (D2ServicesCreator) to switch a letter template off without choosing another version.
/// </summary>
public class DeactivateProjectVersionCommandHandlerTests
{
    private readonly IRepository<Project, Guid> _projectRepository = Substitute.For<IRepository<Project, Guid>>();
    private readonly IRepository<ProjectVersion, Guid> _versionRepository = Substitute.For<IRepository<ProjectVersion, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateProjectVersionCommandHandler _sut;

    public DeactivateProjectVersionCommandHandlerTests()
    {
        _sut = new DeactivateProjectVersionCommandHandler(
            _projectRepository,
            _versionRepository,
            _unitOfWork,
            Substitute.For<ILogger<DeactivateProjectVersionCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WithActiveVersion_DeactivatesVersionAndClearsProjectPointer()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), null, "PRJ-TEST", "Test");
        var version = new ProjectVersion(Guid.NewGuid(), project.Id);
        version.Activate();
        project.SetActiveVersion(version.Id);
        SetupRepositories(project, version);

        // Act
        var result = await _sut.Handle(
            new DeactivateProjectVersionCommand { ProjectId = project.Id, VersionId = version.Id },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.Status.Should().Be(ProjectStatus.Inactive);
        version.IsActive.Should().BeFalse();
        project.CurrentActiveVersionId.Should().BeNull();
        await _versionRepository.Received(1).UpdateAsync(version, Arg.Any<CancellationToken>());
        await _projectRepository.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVersionAlreadyInactive_IsNoOpAndDoesNotSave()
    {
        // Arrange — a Draft version that was never activated must stay Draft.
        var project = new Project(Guid.NewGuid(), null, "PRJ-TEST", "Test");
        var version = new ProjectVersion(Guid.NewGuid(), project.Id);
        SetupRepositories(project, version);

        // Act
        var result = await _sut.Handle(
            new DeactivateProjectVersionCommand { ProjectId = project.Id, VersionId = version.Id },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.Status.Should().Be(ProjectStatus.Draft);
        await _versionRepository.DidNotReceive().UpdateAsync(Arg.Any<ProjectVersion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProjectStillPointsAtInactiveVersion_ClearsPointer()
    {
        // Arrange — inconsistent state: flag already false, but projects.current_active_version_id still set.
        var project = new Project(Guid.NewGuid(), null, "PRJ-TEST", "Test");
        var version = new ProjectVersion(Guid.NewGuid(), project.Id);
        project.SetActiveVersion(version.Id);
        SetupRepositories(project, version);

        // Act
        var result = await _sut.Handle(
            new DeactivateProjectVersionCommand { ProjectId = project.Id, VersionId = version.Id },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        project.CurrentActiveVersionId.Should().BeNull();
        await _projectRepository.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns((Project?)null);

        // Act
        var result = await _sut.Handle(
            new DeactivateProjectVersionCommand { ProjectId = projectId, VersionId = Guid.NewGuid() },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVersionBelongsToAnotherProject_ReturnsFailureWithoutChanges()
    {
        // Arrange
        var project = new Project(Guid.NewGuid(), null, "PRJ-TEST", "Test");
        var foreignVersion = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid());
        foreignVersion.Activate();
        SetupRepositories(project, foreignVersion);

        // Act
        var result = await _sut.Handle(
            new DeactivateProjectVersionCommand { ProjectId = project.Id, VersionId = foreignVersion.Id },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not belong");
        foreignVersion.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void SetupRepositories(Project project, ProjectVersion version)
    {
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _versionRepository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        _versionRepository.FindAsync(Arg.Any<Expression<Func<ProjectVersion, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectVersion> { version });
    }
}
