using System.Linq.Expressions;
using System.Text.Json;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Features.Projects.Commands.CloneProjectVersion;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Projects;

/// <summary>
/// Editing an active letter must not change it in place: the GUI clones it into a new draft
/// (next minor number, same step data, current template file, all workers) and edits the copy.
/// </summary>
public class CloneProjectVersionCommandHandlerTests
{
    private readonly IRepository<Project, Guid> _projectRepository = Substitute.For<IRepository<Project, Guid>>();
    private readonly IRepository<ProjectVersion, Guid> _versionRepository = Substitute.For<IRepository<ProjectVersion, Guid>>();
    private readonly IFileRepository _fileRepository = Substitute.For<IFileRepository>();
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CloneProjectVersionCommandHandler _sut;

    public CloneProjectVersionCommandHandlerTests()
    {
        _sut = new CloneProjectVersionCommandHandler(
            _projectRepository, _versionRepository, _fileRepository, _workerRepository, _unitOfWork,
            Substitute.For<ILogger<CloneProjectVersionCommandHandler>>());
    }

    private void Setup(Project project, ProjectVersion source, IEnumerable<ProjectVersion> all, IEnumerable<FileEntity>? files = null, IEnumerable<Worker>? workers = null)
    {
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _versionRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        _versionRepository.FindAsync(Arg.Any<Expression<Func<ProjectVersion, bool>>>(), Arg.Any<CancellationToken>()).Returns(all.ToList());
        _fileRepository.GetByProjectVersionIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns((files ?? Array.Empty<FileEntity>()).ToList());
        _workerRepository.GetByProjectVersionIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns((workers ?? Array.Empty<Worker>()).ToList());
    }

    [Fact]
    public async Task Handle_ActiveVersion_CreatesNextMinorDraftWithCopiedData()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-A", "Umowa");
        var source = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 0);
        source.Activate();
        source.UpdateStepData(JsonDocument.Parse("""{"general":{"documentName":"Umowa"},"templateAndSchema":{"templateId":"szablon.docx"}}"""));
        project.SetActiveVersion(source.Id);
        var older = new FileEntity(Guid.NewGuid(), source.Id, "stary.docx", null, storageRef: "templates/a/old");
        var newer = new FileEntity(Guid.NewGuid(), source.Id, "szablon.docx", null, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1234, "sha", "templates/a/new");
        typeof(FileEntity).GetProperty(nameof(FileEntity.CreatedAt))!.SetValue(older, DateTime.UtcNow.AddHours(-1));
        var workers = new[]
        {
            new Worker(Guid.NewGuid(), source.Id, "firmowego", "Podstaw wartość", JsonDocument.Parse("""{"search_for":"<%firmowego%>","replacement_property_expression":"formModel.a"}""")),
            new Worker(Guid.NewGuid(), source.Id, "zgoda", "Stan", JsonDocument.Parse("""{"tag_name":"zgoda","conditional_expression":"formModel.zgoda"}""")),
        };
        Setup(project, source, new[] { source }, new[] { older, newer }, workers);

        ProjectVersion? added = null;
        FileEntity? addedFile = null;
        var addedWorkers = new List<Worker>();
        _versionRepository.AddAsync(Arg.Do<ProjectVersion>(v => added = v), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<ProjectVersion>());
        _fileRepository.AddAsync(Arg.Do<FileEntity>(f => addedFile = f), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<FileEntity>());
        _workerRepository.AddAsync(Arg.Do<Worker>(w => addedWorkers.Add(w)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Worker>());

        var result = await _sut.Handle(new CloneProjectVersionCommand { ProjectId = project.Id, SourceVersionId = source.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.VersionTag.Should().Be("1.1");
        result.Value.IsActive.Should().BeFalse();
        result.Value.Status.Should().Be(ProjectStatus.Draft);
        added.Should().NotBeNull();
        added!.Id.Should().NotBe(source.Id);
        added.StepData!.RootElement.GetProperty("templateAndSchema").GetProperty("templateId").GetString().Should().Be("szablon.docx");
        added.Notes.Should().Contain("1.0");
        addedFile.Should().NotBeNull();
        addedFile!.ProjectVersionId.Should().Be(added.Id);
        addedFile.Filename.Should().Be("szablon.docx");
        addedFile.StorageRef.Should().Be("templates/a/new");
        addedWorkers.Select(w => w.Name).Should().BeEquivalentTo("firmowego", "zgoda");
        addedWorkers.Should().OnlyContain(w => w.ProjectVersionId == added.Id);
        addedWorkers[0].ConfigJson.RootElement.GetProperty("search_for").GetString().Should().Be("<%firmowego%>");
        source.IsActive.Should().BeTrue("source version must stay untouched");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NumbersAfterHighestExistingVersion()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-B", "Pismo");
        var source = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 0);
        var later = new ProjectVersion(Guid.NewGuid(), project.Id, 2, 3);
        Setup(project, source, new[] { source, later });
        _versionRepository.AddAsync(Arg.Any<ProjectVersion>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<ProjectVersion>());

        var result = await _sut.Handle(new CloneProjectVersionCommand { ProjectId = project.Id, SourceVersionId = source.Id }, CancellationToken.None);

        result.Value!.VersionTag.Should().Be("2.4");
    }

    [Fact]
    public async Task Handle_VersionFromAnotherProject_Fails()
    {
        var project = new Project(Guid.NewGuid(), null, "PRJ-C", "Obce");
        var foreign = new ProjectVersion(Guid.NewGuid(), Guid.NewGuid(), 1, 0);
        Setup(project, foreign, Array.Empty<ProjectVersion>());

        var result = await _sut.Handle(new CloneProjectVersionCommand { ProjectId = project.Id, SourceVersionId = foreign.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _versionRepository.DidNotReceive().AddAsync(Arg.Any<ProjectVersion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void NextVersionNumber_NoVersions_Returns1_0()
    {
        CloneProjectVersionCommandHandler.NextVersionNumber(Array.Empty<ProjectVersion>()).Should().Be((1, 0));
    }
}
