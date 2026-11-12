using System.Text.Json;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Application.Features.WorkerProfiles.Commands;
using D2ApiCreator.Application.Validators.WorkerProfiles;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.WorkerProfiles;

/// <summary>
/// BR-016: synchronizacja z katalogiem aplikacji docelowej dokłada nowe typy jako ukryte, nie nadpisuje
/// edycji administratora i tylko oznacza typy, których katalog już nie zwraca.
/// </summary>
public class SyncWorkerProfilesCommandHandlerTests
{
    private readonly IWorkerProfileRepository _repository = Substitute.For<IWorkerProfileRepository>();
    private readonly IWorkerCatalogSource _catalog = Substitute.For<IWorkerCatalogSource>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SyncWorkerProfilesCommandHandler _sut;

    public SyncWorkerProfilesCommandHandlerTests()
    {
        _catalog.Name.Returns("test-catalog");
        _sut = new SyncWorkerProfilesCommandHandler(_repository, _catalog, _unitOfWork, Substitute.For<ILogger<SyncWorkerProfilesCommandHandler>>());
    }

    private static WorkerProfile Profile(string key, bool visible, WorkerProfileSource source, string displayName = "")
        => new(Guid.NewGuid(), key, displayName == "" ? key : displayName, "opis admina", JsonDocument.Parse("""{"a":1}"""), visible, 1, source);

    [Fact]
    public async Task Handle_NewTypes_AreAddedHidden_ExistingKeepAdminEdits_MissingAreFlagged()
    {
        var stan = Profile("Stan", visible: true, WorkerProfileSource.Synced, displayName: "Stan (widoczność)");
        var stary = Profile("Stary typ", visible: true, WorkerProfileSource.Synced);
        var reczny = Profile("Mój ręczny", visible: true, WorkerProfileSource.Manual);
        _repository.GetAllOrderedAsync(Arg.Any<CancellationToken>()).Returns(new List<WorkerProfile> { stan, stary, reczny });
        _catalog.GetWorkerTypesAsync(Arg.Any<CancellationToken>()).Returns(new List<WorkerCatalogItem>
        {
            new("Stan", "Stan", "opis z katalogu", """{"tag_name":"{name}"}"""),
            new("Usuń obszar", "Usuń obszar", "usuwa obszar", """{"tag_name":"{name}","remove_when":false}"""),
            new("Zepsuty", "Zepsuty", null, "nie-json"),
        });
        var added = new List<WorkerProfile>();
        _repository.AddAsync(Arg.Do<WorkerProfile>(p => added.Add(p)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<WorkerProfile>());

        var result = await _sut.Handle(new SyncWorkerProfilesCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error);
        var dto = result.Value!;
        dto.Added.Should().BeEquivalentTo("Usuń obszar", "Zepsuty");
        dto.Confirmed.Should().BeEquivalentTo("Stan");
        dto.Missing.Should().BeEquivalentTo("Stary typ");
        dto.Total.Should().Be(5);

        added.Should().OnlyContain(p => !p.IsVisible && p.Source == WorkerProfileSource.Synced && !p.IsMissingInSource);
        added.Single(p => p.Key == "Zepsuty").DefaultConfigJson.RootElement.GetRawText().Should().Be("{}");
        added.Single(p => p.Key == "Usuń obszar").DefaultConfigJson.RootElement.GetProperty("remove_when").GetBoolean().Should().BeFalse();

        stan.DisplayName.Should().Be("Stan (widoczność)", "edycje administratora nie są nadpisywane");
        stan.Description.Should().Be("opis admina");
        stan.IsVisible.Should().BeTrue();
        stan.IsMissingInSource.Should().BeFalse();
        stan.LastSyncedAt.Should().NotBeNull();

        stary.IsMissingInSource.Should().BeTrue();
        stary.IsVisible.Should().BeTrue("brak w katalogu nie ukrywa typu automatycznie");
        reczny.IsMissingInSource.Should().BeFalse("ręcznych profili synchronizacja nie oznacza");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ManualProfileFoundInCatalog_BecomesSynced()
    {
        var reczny = Profile("Usuń obszar", visible: false, WorkerProfileSource.Manual);
        _repository.GetAllOrderedAsync(Arg.Any<CancellationToken>()).Returns(new List<WorkerProfile> { reczny });
        _catalog.GetWorkerTypesAsync(Arg.Any<CancellationToken>()).Returns(new List<WorkerCatalogItem> { new("Usuń obszar", "Usuń obszar", null, null) });

        var result = await _sut.Handle(new SyncWorkerProfilesCommand(), CancellationToken.None);

        result.Value!.Confirmed.Should().Equal("Usuń obszar");
        reczny.Source.Should().Be(WorkerProfileSource.Synced);
    }

    [Fact]
    public async Task Handle_CatalogUnavailable_FailsWithoutSaving()
    {
        _catalog.GetWorkerTypesAsync(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<WorkerCatalogItem>>(_ => throw new HttpRequestException("timeout"));

        var result = await _sut.Handle(new SyncWorkerProfilesCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("test-catalog");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("{}", true)]
    [InlineData("""{"search_for":"<%{name}%>"}""", true)]
    [InlineData("[1,2]", false)]
    [InlineData("nie-json", false)]
    public void Validator_DefaultConfigJson_MustBeJsonObject(string json, bool valid)
    {
        var result = new UpdateWorkerProfileDtoValidator().Validate(new UpdateWorkerProfileDto { DisplayName = "x", DefaultConfigJson = json });

        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task Delete_SyncedProfile_IsRefused()
    {
        var synced = Profile("Stan", true, WorkerProfileSource.Synced);
        _repository.GetByIdAsync(synced.Id, Arg.Any<CancellationToken>()).Returns(synced);
        var handler = new DeleteWorkerProfileCommandHandler(_repository, _unitOfWork);

        var result = await handler.Handle(new DeleteWorkerProfileCommand(synced.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<WorkerProfile>(), Arg.Any<CancellationToken>());
    }
}
