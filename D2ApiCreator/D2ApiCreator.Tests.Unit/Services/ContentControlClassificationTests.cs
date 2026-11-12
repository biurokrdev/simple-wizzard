using D2ApiCreator.Application.Common.Interfaces;
using DocumentFormat.OpenXml;
using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Application.Features.Workers.Commands.GenerateWorkers;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Infrastructure.Services;
using D2ApiCreator.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using static D2ApiCreator.Tests.Unit.Helpers.DocxContentControlTestHelper;

namespace D2ApiCreator.Tests.Unit.Services;

/// <summary>
/// Rozpoznawanie rodzaju formantu w szablonie: wariant (Stan), sekcja powtarzana, lista punktowana / numerowana, tabela.
/// Słownik <c>ContentControls</c> zostaje zgodny wstecz ("boolean" dla wariantu).
/// </summary>
public class ContentControlClassificationTests
{
    private readonly DocumentPlaceholderService _sut = new(Substitute.For<ILogger<DocumentPlaceholderService>>());

    [Fact]
    public async Task PlainBlockControl_IsVariant_AndDictionaryStaysBoolean()
    {
        using var stream = CreateDocx(BlockSdt("zgoda_marketing", Para("Wyrażam zgodę na kontakt marketingowy.")));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");
        stream.Position = 0;
        var dictionary = await _sut.ExtractContentControlsFromDocxAsync(stream, "t.docx");

        var d = details.Should().ContainSingle().Subject;
        d.Name.Should().Be("zgoda_marketing");
        d.Kind.Should().Be(ContentControlKinds.Variant);
        d.Level.Should().Be(ContentControlLevels.Block);
        d.Location.Should().Be("MainDocument");
        d.Preview.Should().StartWith("Wyrażam zgodę");
        d.ParagraphCount.Should().Be(1);
        d.SuggestedWorkerType.Should().Be("Stan");
        dictionary.Should().BeEquivalentTo(new Dictionary<string, string> { ["zgoda_marketing"] = "boolean" });
    }

    [Fact]
    public async Task RepeatingSection_IsRepeat_AndItemIsNotListedSeparately()
    {
        using var stream = CreateDocx(RepeatingSectionSdt("pozycje", Para("Pozycja <%nazwa%> — <%kwota%>")));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");
        stream.Position = 0;
        var dictionary = await _sut.ExtractContentControlsFromDocxAsync(stream, "t.docx");

        var d = details.Should().ContainSingle().Subject;
        d.Name.Should().Be("pozycje");
        d.Kind.Should().Be(ContentControlKinds.Repeat);
        d.IsRepeating.Should().BeTrue();
        d.SuggestedWorkerType.Should().Be("Powtórz blok");
        dictionary["pozycje"].Should().Be("array");
    }

    [Theory]
    [InlineData("raty[]")]
    [InlineData("repeat:raty")]
    [InlineData("lista:raty")]
    public async Task NamingConvention_MarksControlAsRepeat(string tag)
    {
        using var stream = CreateDocx(BlockSdt(tag, Para("Rata <%numer%>")));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");

        details.Single().Kind.Should().Be(ContentControlKinds.Repeat);
        details.Single().Name.Should().Be(tag);
    }

    [Fact]
    public async Task ControlWrappingTable_IsTable_AndRowControl_IsTableAtRowLevel()
    {
        using var stream = CreateDocx(
            BlockSdt("tabela_oplat", SimpleTable("Opłata", "Kwota")),
            TableWithRowSdt("wiersz_oplaty", "<%nazwa%>", "<%kwota%>"));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");

        details.Should().HaveCount(2);
        var block = details.Single(d => d.Name == "tabela_oplat");
        block.Kind.Should().Be(ContentControlKinds.Table);
        block.ContainsTable.Should().BeTrue();
        block.Level.Should().Be(ContentControlLevels.Block);
        var row = details.Single(d => d.Name == "wiersz_oplaty");
        row.Kind.Should().Be(ContentControlKinds.Table);
        row.Level.Should().Be(ContentControlLevels.Row);
    }

    [Fact]
    public async Task NumberedParagraphs_AreList_WithBulletOrNumberedType()
    {
        using var stream = CreateDocx(
            BlockSdt("zalaczniki", ListItem("Umowa", BulletNumId), ListItem("Regulamin", BulletNumId)),
            BlockSdt("kroki", ListItem("Podpisz", DecimalNumId), ListItem("Odeślij", DecimalNumId)),
            BlockSdt("mieszane", ListItem("Punkt", BulletNumId), Para("zwykły akapit")));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");

        details.Single(d => d.Name == "zalaczniki").Should().BeEquivalentTo(new { Kind = ContentControlKinds.List, ListType = "bullet", ContainsList = true, ParagraphCount = 2 });
        details.Single(d => d.Name == "kroki").Should().BeEquivalentTo(new { Kind = ContentControlKinds.List, ListType = "numbered" });
        details.Single(d => d.Name == "mieszane").Kind.Should().Be(ContentControlKinds.Variant, "lista tylko gdy wszystkie akapity są numerowane");
    }

    [Fact]
    public async Task RepeatWinsOverTableAndList()
    {
        using var stream = CreateDocx(RepeatingSectionSdt("wiersze", SimpleTable("<%a%>", "<%b%>")));

        var d = (await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx")).Single();

        d.Kind.Should().Be(ContentControlKinds.Repeat);
        d.ContainsTable.Should().BeTrue();
    }

    [Fact]
    public async Task InlineControl_IsVariantAtInlineLevel_AndNestedControlKnowsItsParent()
    {
        using var stream = CreateDocx(
            ParagraphWithInlineSdt("Szanowny ", "tytul", "Panie"),
            RepeatingSectionSdt("pozycje", BlockSdt("rabat", Para("Rabat <%rabat%>"))));

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");

        details.Should().HaveCount(3);
        var inline = details.Single(d => d.Name == "tytul");
        inline.Kind.Should().Be(ContentControlKinds.Variant);
        inline.Level.Should().Be(ContentControlLevels.Inline);
        var nested = details.Single(d => d.Name == "rabat");
        nested.Kind.Should().Be(ContentControlKinds.Variant);
        nested.Parent.Should().Be("pozycje", "egzemplarz sekcji nie ma tagu, więc rodzicem jest sekcja");
    }

    [Fact]
    public async Task CheckBox_IsSkipped_AndHeaderControl_HasHeaderLocation()
    {
        using var stream = CreateDocx(
            new OpenXmlElement[] { CheckBoxSdt("chk_zgoda"), BlockSdt("tresc", Para("x")) },
            new OpenXmlElement[] { BlockSdt("naglowek_wariant", Para("Oddział <%miasto%>")) });

        var details = await _sut.ExtractContentControlDetailsFromDocxAsync(stream, "t.docx");

        details.Select(d => d.Name).Should().BeEquivalentTo("tresc", "naglowek_wariant");
        details.Single(d => d.Name == "naglowek_wariant").Location.Should().Be("Header");
    }

    [Fact]
    public async Task GenerateWorkers_UsesKind_ToPickWorkerTypeAndRepeatScope()
    {
        var versionId = Guid.NewGuid();
        var workers = Substitute.For<IWorkerRepository>();
        var versions = Substitute.For<IProjectVersionRepository>();
        versions.GetByIdAsync(versionId, Arg.Any<CancellationToken>()).Returns(new ProjectVersion(versionId, Guid.NewGuid()));
        var handler = new GenerateWorkersCommandHandler(workers, versions, Substitute.For<IUnitOfWork>(), Substitute.For<ILogger<GenerateWorkersCommandHandler>>());

        var result = await handler.Handle(new GenerateWorkersCommand(new GenerateWorkersDto
        {
            ProjectVersionId = versionId,
            Placeholders = new List<string> { "<%kwota%>" },
            ContentControls = new Dictionary<string, string>
            {
                ["zgoda"] = "boolean",
                ["pozycje"] = "array",
                ["tabela"] = "array",
                ["zalaczniki"] = "array",
                ["stary_klient_bez_detali"] = "array"
            },
            ContentControlDetails = new List<ContentControlDetailDto>
            {
                new() { Name = "zgoda", Kind = ContentControlKinds.Variant },
                new() { Name = "pozycje", Kind = ContentControlKinds.Repeat },
                new() { Name = "tabela", Kind = ContentControlKinds.Table },
                new() { Name = "zalaczniki", Kind = ContentControlKinds.List, ListType = "bullet" }
            }
        }), CancellationToken.None);

        result.Should().HaveCount(6);
        result.Single(w => w.Name == "kwota").WorkerType.Should().Be("Podstaw wartość");
        result.Single(w => w.Name == "zgoda").WorkerType.Should().Be("Stan");
        result.Single(w => w.Name == "zgoda").ConfigJson.Should().Contain("conditional_expression");
        result.Single(w => w.Name == "pozycje").WorkerType.Should().Be("Powtórz blok");
        result.Single(w => w.Name == "pozycje").ConfigJson.Should().Contain("\"repeat_scope\":\"block\"").And.Contain("formModel.pozycje");
        result.Single(w => w.Name == "tabela").ConfigJson.Should().Contain("\"repeat_scope\":\"row\"");
        result.Single(w => w.Name == "zalaczniki").ConfigJson.Should().Contain("\"repeat_scope\":\"paragraph\"");
        result.Single(w => w.Name == "stary_klient_bez_detali").WorkerType.Should().Be("Powtórz blok", "bez szczegółów decyduje typ w schemacie");
    }

    [Fact]
    public void ResolveContentControlWorkerType_WithoutDetails_IsBackwardCompatible()
    {
        GenerateWorkersCommandHandler.ResolveContentControlWorkerType("boolean", null).Should().Be("Stan");
        GenerateWorkersCommandHandler.ResolveContentControlWorkerType("array", null).Should().Be("Powtórz blok");
        DocumentPlaceholderService.HasRepeatNamingConvention("zwykly_tag").Should().BeFalse();
    }
}
