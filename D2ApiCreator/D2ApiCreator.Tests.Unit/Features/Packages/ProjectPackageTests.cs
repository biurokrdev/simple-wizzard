using System.IO.Compression;
using System.Text;
using System.Text.Json;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Packages;
using D2ApiCreator.Application.Features.Packages;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Packages;

/// <summary>BR-022: paczka wdrożeniowa = manifest + wszystkie workery + szablon wersji w jednym ZIP-ie; wysyłka do aplikacji docelowej.</summary>
public class ProjectPackageTests
{
    private static readonly byte[] Docx = Encoding.UTF8.GetBytes("PK-fake-docx-content");

    private static (Project project, ProjectVersion version, List<Worker> workers, FileEntity template) Fixture()
    {
        var project = new Project(Guid.NewGuid(), Guid.NewGuid(), "UmowaKredytowa", "Umowa kredytowa", new ProjectMetadata(applicationName: "Okapi"), 120);
        var version = new ProjectVersion(Guid.NewGuid(), project.Id, 1, 2);
        var workers = new List<Worker>
        {
            new(Guid.NewGuid(), version.Id, "kwota", "Podstaw wartość", JsonDocument.Parse("""{"search_for":"<%kwota%>","replacement_property_expression":"formModel.kwota"}""")),
            new(Guid.NewGuid(), version.Id, "zgoda", "Stan", JsonDocument.Parse("""{"tag_name":"zgoda","conditional_expression":"formModel.zgoda"}""")),
            new(Guid.NewGuid(), version.Id, "pozycje", "Powtórz blok", JsonDocument.Parse("""{"tag_name":"pozycje","collection_expression":"formModel.pozycje","repeat_scope":"row"}"""))
        };
        var template = new FileEntity(Guid.NewGuid(), version.Id, "umowa/kredyt v2.docx", null, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Docx.Length, null, "templates/x/y");
        return (project, version, workers, template);
    }

    [Fact]
    public void Build_ProducesZipWithManifestWorkersAndTemplate()
    {
        var (project, version, workers, template) = Fixture();

        var package = ProjectPackageBuilder.Build(project, version, workers, template, Docx, "Kredyty", "User", new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc));

        package.FileName.Should().Be("UmowaKredytowa_v1.2.zip");
        package.Sha256.Should().HaveLength(64);

        using var zip = new ZipArchive(new MemoryStream(package.Content), ZipArchiveMode.Read);
        zip.Entries.Select(e => e.FullName).Should().BeEquivalentTo("manifest.json", "workers.json", "template/umowa_kredyt v2.docx");

        using var manifestDoc = JsonDocument.Parse(Read(zip, "manifest.json"));
        var root = manifestDoc.RootElement;
        root.GetProperty("format").GetString().Should().Be("d2creator-package/1");
        root.GetProperty("project").GetProperty("source").GetString().Should().Be("UmowaKredytowa");
        root.GetProperty("project").GetProperty("tenantName").GetString().Should().Be("Kredyty");
        root.GetProperty("project").GetProperty("applicationName").GetString().Should().Be("Okapi");
        root.GetProperty("version").GetProperty("tag").GetString().Should().Be("1.2");
        root.GetProperty("template").GetProperty("entry").GetString().Should().Be("template/umowa_kredyt v2.docx");
        root.GetProperty("template").GetProperty("kind").GetString().Should().Be("docx");
        root.GetProperty("template").GetProperty("sha256").GetString().Should().Be(ProjectPackageBuilder.Sha256Hex(Docx));
        root.GetProperty("workers").GetProperty("count").GetInt32().Should().Be(3);
        root.GetProperty("workers").GetProperty("byType").GetProperty("Stan").GetInt32().Should().Be(1);

        using var workersDoc = JsonDocument.Parse(Read(zip, "workers.json"));
        var list = workersDoc.RootElement.GetProperty("workers").EnumerateArray().ToList();
        list.Should().HaveCount(3);
        var kwota = list.Single(w => w.GetProperty("name").GetString() == "kwota");
        kwota.GetProperty("workerType").GetString().Should().Be("Podstaw wartość");
        kwota.GetProperty("config").GetProperty("search_for").GetString().Should().Be("<%kwota%>", "konfiguracja jest obiektem JSON, nie stringiem");

        using var tpl = zip.GetEntry("template/umowa_kredyt v2.docx")!.Open();
        using var ms = new MemoryStream();
        tpl.CopyTo(ms);
        ms.ToArray().Should().Equal(Docx);
    }

    [Fact]
    public void Build_WithoutTemplate_HasNoTemplateEntry_AndManifestTemplateIsNull()
    {
        var (project, version, workers, _) = Fixture();

        var package = ProjectPackageBuilder.Build(project, version, workers, null, null, null, null);

        using var zip = new ZipArchive(new MemoryStream(package.Content), ZipArchiveMode.Read);
        zip.Entries.Select(e => e.FullName).Should().BeEquivalentTo("manifest.json", "workers.json");
        package.Manifest.Template.Should().BeNull();
    }

    [Fact]
    public async Task BuildQuery_FailsWhenTemplateBytesAreMissingInStorage()
    {
        var (project, version, workers, template) = Fixture();
        typeof(ProjectVersion).GetField("_files", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(version, new List<FileEntity> { template });
        var projects = Substitute.For<IRepository<Project, Guid>>();
        projects.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var versions = Substitute.For<IProjectVersionRepository>();
        versions.GetWithDetailsAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        var workerRepo = Substitute.For<IWorkerRepository>();
        workerRepo.GetByProjectVersionIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(workers);
        var storage = Substitute.For<IFileStorageService>();
        storage.DownloadAsync("templates/x/y", Arg.Any<CancellationToken>()).Returns<byte[]>(_ => throw new InvalidOperationException("object not found"));
        var handler = new BuildProjectPackageQueryHandler(projects, versions, workerRepo, Substitute.For<IRepository<Tenant, Guid>>(), storage, Substitute.For<ILogger<BuildProjectPackageQueryHandler>>());

        var result = await handler.Handle(new BuildProjectPackageQuery(project.Id, version.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not available in storage");

        storage.DownloadAsync("templates/x/y", Arg.Any<CancellationToken>()).Returns(Docx);
        var ok = await handler.Handle(new BuildProjectPackageQuery(project.Id, version.Id, "jan"), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue(ok.Error);
        ok.Value!.Manifest.Template!.FileName.Should().Be("umowa/kredyt v2.docx");
        ok.Value.Manifest.Workers.Count.Should().Be(3);
    }

    [Fact]
    public async Task Publish_WithoutConfiguredTarget_FailsWithoutBuilding()
    {
        var sender = Substitute.For<ISender>();
        var delivery = Substitute.For<IPackageDeliveryService>();
        delivery.IsConfigured.Returns(false);
        var handler = new PublishProjectPackageCommandHandler(sender, delivery, Substitute.For<IRepository<ProjectVersion, Guid>>(), Substitute.For<IUnitOfWork>(), Substitute.For<ILogger<PublishProjectPackageCommandHandler>>());

        var result = await handler.Handle(new PublishProjectPackageCommand(Guid.NewGuid(), Guid.NewGuid(), "jan"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not configured");
        await sender.DidNotReceive().Send(Arg.Any<BuildProjectPackageQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_Success_MarksVersionDelivered_AndRejection_DoesNot()
    {
        var (project, version, workers, template) = Fixture();
        var package = ProjectPackageBuilder.Build(project, version, workers, template, Docx, null, "jan");
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<BuildProjectPackageQuery>(), Arg.Any<CancellationToken>()).Returns(Application.Common.Result<ProjectPackage>.Success(package));
        var delivery = Substitute.For<IPackageDeliveryService>();
        delivery.IsConfigured.Returns(true);
        delivery.Target.Returns("https://okapi.example/api/packages");
        var versions = Substitute.For<IRepository<ProjectVersion, Guid>>();
        versions.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new PublishProjectPackageCommandHandler(sender, delivery, versions, unitOfWork, Substitute.For<ILogger<PublishProjectPackageCommandHandler>>());

        delivery.SendAsync(package, Arg.Any<CancellationToken>()).Returns(new PackageDeliveryResult(false, "https://okapi.example/api/packages", "400 Bad Request", "400 Bad Request: schema"));
        var rejected = await handler.Handle(new PublishProjectPackageCommand(project.Id, version.Id, "jan"), CancellationToken.None);
        rejected.IsSuccess.Should().BeFalse();
        rejected.Error.Should().StartWith("Target application rejected");
        version.DeliveredAt.Should().BeNull();

        delivery.SendAsync(package, Arg.Any<CancellationToken>()).Returns(new PackageDeliveryResult(true, "https://okapi.example/api/packages", "202 Accepted", null));
        var ok = await handler.Handle(new PublishProjectPackageCommand(project.Id, version.Id, "jan"), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue(ok.Error);
        ok.Value!.WorkerCount.Should().Be(3);
        ok.Value.HasTemplate.Should().BeTrue();
        ok.Value.DeliveredTo.Should().Be("https://okapi.example/api/packages");
        version.DeliveredAt.Should().NotBeNull();
        version.DeliveredPackageSha.Should().Be(package.Sha256);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static string Read(ZipArchive zip, string entry)
    {
        using var reader = new StreamReader(zip.GetEntry(entry)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
