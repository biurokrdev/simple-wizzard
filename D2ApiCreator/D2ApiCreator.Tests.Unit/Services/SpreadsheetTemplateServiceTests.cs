using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
using D2ApiCreator.Application.Features.Workers.Commands.GenerateWorkers;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Infrastructure.Services;
using D2ApiCreator.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using static D2ApiCreator.Tests.Unit.Helpers.XlsxTestHelper;

namespace D2ApiCreator.Tests.Unit.Services;

/// <summary>
/// Szablon XLSX (BR-021): luźny znacznik z etykietą obok → „Podstaw wartość”; wiersz ze stałym tekstem nad wierszem
/// znaczników → nagłówek; wiersz z samymi znacznikami → element powielany z kolumnami mapowanymi na właściwości elementu.
/// </summary>
public class SpreadsheetTemplateServiceTests
{
    private readonly SpreadsheetTemplateService _sut = new(Substitute.For<ILogger<SpreadsheetTemplateService>>());

    [Fact]
    public async Task LoosePlaceholder_WithLabelOnTheLeft_IsSubstitutionWithLabelAndCell()
    {
        using var stream = CreateXlsx("Koszty",
            Row(("A", "Koszt"), ("B", "<%koszt_calkowity%>")),
            Row(("A", "Data"), ("C", "<%data_raportu%>")));

        var result = await _sut.AnalyzeAsync(stream, "k.xlsx");

        result.Placeholders.Should().BeEquivalentTo("<%data_raportu%>", "<%koszt_calkowity%>");
        result.Tables.Should().BeEmpty();
        var koszt = result.PlaceholderDetails.Single(d => d.Placeholder == "<%koszt_calkowity%>");
        koszt.Should().BeEquivalentTo(new { Label = "Koszt", Sheet = "Koszty", Cell = "B1" });
        result.PlaceholderDetails.Single(d => d.Placeholder == "<%data_raportu%>").Label.Should().Be("Data", "etykieta to najbliższa stała komórka po lewej");
    }

    [Fact]
    public async Task HeaderRowAbovePlaceholderRow_IsTableWithColumnsMappedToItemProperties()
    {
        using var stream = CreateXlsx("Pozycje",
            Row(("A", "Lp"), ("B", "Nazwa"), ("C", "Kwota")),
            Row(("A", "<%pozycje_lp%>"), ("B", "<%pozycje_nazwa%>"), ("C", "<%pozycje_kwota%>")),
            Row(("A", "Razem"), ("C", "<%suma%>")));

        var result = await _sut.AnalyzeAsync(stream, "p.xlsx");

        var table = result.Tables.Should().ContainSingle().Subject;
        table.Name.Should().Be("pozycje", "wspólny przedrostek znaczników");
        table.Kind.Should().Be(ContentControlKinds.Table);
        table.Level.Should().Be(ContentControlLevels.Row);
        table.Sheet.Should().Be("Pozycje");
        table.Range.Should().Be("A2:C2");
        table.HeaderCells.Should().Equal("Lp", "Nazwa", "Kwota");
        table.Columns!.Select(c => (c.Cell, c.Property, c.Header)).Should().Equal(("A2", "lp", "Lp"), ("B2", "nazwa", "Nazwa"), ("C2", "kwota", "Kwota"));
        table.Preview.Should().Be("Lp | Nazwa | Kwota → <%pozycje_lp%> | <%pozycje_nazwa%> | <%pozycje_kwota%>");

        result.Placeholders.Should().Equal(new[] { "<%suma%>" }, "znaczniki wiersza powielanego nie są luźnymi znacznikami");
        result.PlaceholderDetails.Single().Label.Should().Be("Razem");
    }

    [Fact]
    public async Task PlaceholderRowWithoutCommonPrefix_GetsSheetBasedName_AndRawPropertyNames()
    {
        using var stream = CreateXlsx("Arkusz 1",
            Row(("A", "<%lubie_do_buzi%>"), ("B", "<%ilosc_razy%>"), ("C", "<%z_gumbka%>")));

        var table = (await _sut.AnalyzeAsync(stream, "x.xlsx")).Tables.Single();

        table.Name.Should().Be("wiersz_arkusz_1_1");
        table.HeaderCells.Should().BeNull();
        table.Columns!.Select(c => c.Property).Should().Equal("lubie_do_buzi", "ilosc_razy", "z_gumbka");
    }

    [Fact]
    public async Task SinglePlaceholderRow_IsRepeatedOnlyUnderHeader_OtherwiseLoose()
    {
        using var stream = CreateXlsx("S",
            Row(("A", "Załączniki")),
            Row(("A", "<%zalaczniki%>")),
            Row(("A", "<%podpis%>")));

        var result = await _sut.AnalyzeAsync(stream, "s.xlsx");

        var table = result.Tables.Should().ContainSingle().Subject;
        table.Name.Should().Be("zalaczniki", "jedna kolumna pod nagłówkiem bierze nazwę z nagłówka (bez polskich znaków)");
        table.Columns!.Single().Property.Should().BeNull("jedna kolumna → lista wartości prostych (item)");
        result.Placeholders.Should().Equal("<%podpis%>");
    }

    [Fact]
    public async Task MixedRow_TextAndPlaceholders_IsLoose_AndMultiplePlaceholdersInOneCellAreAllFound()
    {
        using var stream = CreateXlsx("M",
            Row(("A", "Klient"), ("B", "<%imie%> <%nazwisko%>"), ("C", "<%miasto%>")));

        var result = await _sut.AnalyzeAsync(stream, "m.xlsx");

        result.Tables.Should().BeEmpty();
        result.Placeholders.Should().BeEquivalentTo("<%imie%>", "<%nazwisko%>", "<%miasto%>");
        result.PlaceholderDetails.Where(d => d.Cell == "B1").Should().HaveCount(2).And.OnlyContain(d => d.Label == "Klient");
    }

    [Fact]
    public async Task TwoPlaceholderRows_WithSamePrefix_GetDistinctNames()
    {
        using var stream = CreateXlsx("D",
            Row(("A", "<%p_a%>"), ("B", "<%p_b%>")),
            Row(("A", "Przerwa")),
            Row(("A", "<%p_c%>"), ("B", "<%p_d%>")));

        var tables = (await _sut.AnalyzeAsync(stream, "d.xlsx")).Tables;

        tables.Select(t => t.Name).Should().Equal("p", "p_2");
    }

    [Fact]
    public void Sanitize_TransliteratesPolishAndCollapsesSeparators()
    {
        SpreadsheetTemplateService.Sanitize("Załączniki do umowy (2024)").Should().Be("zalaczniki_do_umowy_2024");
        SpreadsheetTemplateService.Sanitize("Arkusz 1").Should().Be("arkusz_1");
    }

    [Fact]
    public void CommonPrefix_StopsAtFirstDifferingSegment()
    {
        SpreadsheetTemplateService.CommonPrefix(new[] { "faktura_pozycje_nazwa", "faktura_pozycje_kwota" }).Should().Be("faktura_pozycje");
        SpreadsheetTemplateService.CommonPrefix(new[] { "a_x", "b_x" }).Should().BeEmpty();
        SpreadsheetTemplateService.CommonPrefix(new[] { "solo_x" }).Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_RoutesXlsxToSpreadsheetAnalysis_AndFillsDictionaryWithArray()
    {
        var docx = Substitute.For<IDocumentPlaceholderService>();
        using var stream = CreateXlsx("Pozycje",
            Row(("A", "Nazwa"), ("B", "Kwota")),
            Row(("A", "<%pozycje_nazwa%>"), ("B", "<%pozycje_kwota%>")),
            Row(("A", "Koszt"), ("B", "<%koszt%>")));
        var handler = new ExtractPlaceholdersQueryHandler(docx, _sut, Substitute.For<ILogger<ExtractPlaceholdersQueryHandler>>());

        var dto = await handler.Handle(new ExtractPlaceholdersQuery { FileStream = stream, FileName = "raport.xlsx" }, CancellationToken.None);

        dto.Source.Should().Be("xlsx");
        dto.Placeholders.Should().Equal("<%koszt%>");
        dto.ContentControls.Should().BeEquivalentTo(new Dictionary<string, string> { ["pozycje"] = "array" });
        dto.ContentControlDetails.Single().Columns.Should().HaveCount(2);
        dto.PlaceholderDetails.Single().Label.Should().Be("Koszt");
        await docx.DidNotReceive().ExtractPlaceholdersFromDocxAsync(Arg.Any<Stream>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GenerateWorkers_ForSpreadsheet_MapsColumnsToItemProperties_AndKeepsLabelOnSubstitution()
    {
        var versionId = Guid.NewGuid();
        var versions = Substitute.For<IProjectVersionRepository>();
        versions.GetByIdAsync(versionId, Arg.Any<CancellationToken>()).Returns(new ProjectVersion(versionId, Guid.NewGuid()));
        var handler = new GenerateWorkersCommandHandler(Substitute.For<IWorkerRepository>(), versions, Substitute.For<IUnitOfWork>(), Substitute.For<ILogger<GenerateWorkersCommandHandler>>());

        var result = await handler.Handle(new GenerateWorkersCommand(new GenerateWorkersDto
        {
            ProjectVersionId = versionId,
            Placeholders = new List<string> { "<%koszt%>" },
            PlaceholderDetails = new List<PlaceholderDetailDto> { new() { Placeholder = "<%koszt%>", Label = "Koszt", Sheet = "Pozycje", Cell = "B3" } },
            ContentControls = new Dictionary<string, string> { ["pozycje"] = "array", ["zalaczniki"] = "array" },
            ContentControlDetails = new List<ContentControlDetailDto>
            {
                new()
                {
                    Name = "pozycje", Kind = ContentControlKinds.Table, Sheet = "Pozycje", Range = "A2:B2",
                    Columns = new List<TemplateColumnDto>
                    {
                        new() { Cell = "A2", Placeholder = "<%pozycje_nazwa%>", Property = "nazwa", Header = "Nazwa" },
                        new() { Cell = "B2", Placeholder = "<%pozycje_kwota%>", Property = "kwota", Header = "Kwota" }
                    }
                },
                new()
                {
                    Name = "zalaczniki", Kind = ContentControlKinds.Table, Sheet = "S", Range = "A2:A2",
                    Columns = new List<TemplateColumnDto> { new() { Cell = "A2", Placeholder = "<%zalaczniki%>", Property = null } }
                }
            }
        }), CancellationToken.None);

        var koszt = result.Single(w => w.Name == "koszt");
        koszt.WorkerType.Should().Be("Podstaw wartość");
        koszt.ConfigJson.Should().Contain("\"label\":\"Koszt\"").And.Contain("\"cell\":\"B3\"").And.Contain("\"sheet\":\"Pozycje\"");

        var pozycje = result.Single(w => w.Name == "pozycje");
        pozycje.WorkerType.Should().Be("Powtórz blok");
        pozycje.ConfigJson.Should().Contain("\"repeat_scope\":\"row\"").And.Contain("\"range\":\"A2:B2\"")
            .And.Contain("\"item_property_expression\":\"item.nazwa\"").And.Contain("\"item_property_expression\":\"item.kwota\"")
            .And.Contain("\"search_for\":\"<%pozycje_kwota%>\"");

        result.Single(w => w.Name == "zalaczniki").ConfigJson.Should().Contain("\"item_property_expression\":\"item\"", "jedna kolumna → lista wartości prostych");
    }
}
