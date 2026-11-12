using System.Security.Cryptography;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Common.Storage;
using D2ApiCreator.Application.Features.Files.Commands.UploadFile;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Files;

/// <summary>
/// Unit tests for UploadFileCommandHandler — binaria idą do IFileStorageService (GCS), baza dostaje tylko metadane + storage_ref.
/// </summary>
public class UploadFileCommandHandlerTests
{
    private static readonly byte[] Content = { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3 };

    private readonly IFileRepository _fileRepository = Substitute.For<IFileRepository>();
    private readonly IProjectVersionRepository _projectVersionRepository = Substitute.For<IProjectVersionRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UploadFileCommandHandler _sut;

    public UploadFileCommandHandlerTests()
    {
        _fileStorage
            .UploadAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(0));

        _sut = new UploadFileCommandHandler(
            _fileRepository,
            _projectVersionRepository,
            _fileStorage,
            _unitOfWork,
            Substitute.For<ILogger<UploadFileCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WithExistingVersion_UploadsToStorageAndPersistsOnlyMetadata()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        _projectVersionRepository.GetByIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(CreateVersion());
        var command = new UploadFileCommand { ProjectVersionId = versionId, File = CreateFormFile("template.docx") };

        FileEntity? saved = null;
        _fileRepository.AddAsync(Arg.Do<FileEntity>(f => saved = f), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<FileEntity>(0));

        // Act
        var fileId = await _sut.Handle(command, CancellationToken.None);

        // Assert
        var expectedKey = FileStorageKeys.Template(versionId, fileId);
        await _fileStorage.Received(1).UploadAsync(
            expectedKey,
            Arg.Is<byte[]>(b => b.SequenceEqual(Content)),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Arg.Any<CancellationToken>());

        saved.Should().NotBeNull();
        saved!.Id.Should().Be(fileId);
        saved.ProjectVersionId.Should().Be(versionId);
        saved.Filename.Should().Be("template.docx");
        saved.Content.Should().BeNull("binaria nie trafiają już do files.content");
        saved.StorageRef.Should().Be(expectedKey);
        saved.IsInExternalStorage.Should().BeTrue();
        saved.Size.Should().Be(Content.Length);
        saved.Sha256.Should().Be(Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVersionDoesNotExist_ThrowsAndDoesNotTouchStorage()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        _projectVersionRepository.GetByIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns((ProjectVersion?)null);
        var command = new UploadFileCommand { ProjectVersionId = versionId, File = CreateFormFile("template.docx") };

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        await _fileStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default, default);
        await _fileRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenDatabaseSaveFails_DeletesOrphanedObjectAndRethrows()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        _projectVersionRepository.GetByIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(CreateVersion());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("db down"));
        var command = new UploadFileCommand { ProjectVersionId = versionId, File = CreateFormFile("template.docx") };

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        var uploadedKey = (string)_fileStorage.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IFileStorageService.UploadAsync))
            .GetArguments()[0]!;
        await _fileStorage.Received(1).DeleteAsync(uploadedKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStorageUploadFails_DoesNotPersistAnything()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        _projectVersionRepository.GetByIdAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(CreateVersion());
        _fileStorage
            .UploadAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new HttpRequestException("gcs unavailable"));
        var command = new UploadFileCommand { ProjectVersionId = versionId, File = CreateFormFile("template.docx") };

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        await _fileRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static ProjectVersion CreateVersion()
        => new(Guid.NewGuid(), Guid.NewGuid(), 1, 0, Guid.NewGuid());

    private static IFormFile CreateFormFile(string fileName)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        file.Length.Returns(Content.Length);
        file.OpenReadStream().Returns(_ => new MemoryStream(Content));
        return file;
    }
}
