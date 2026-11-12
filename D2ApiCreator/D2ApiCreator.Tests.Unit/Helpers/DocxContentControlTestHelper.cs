using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using W14 = DocumentFormat.OpenXml.Office2010.Word;
using W15 = DocumentFormat.OpenXml.Office2013.Word;

namespace D2ApiCreator.Tests.Unit.Helpers;

/// <summary>
/// Buduje dokumenty DOCX z formantami (SDT) różnych rodzajów — do testów klasyfikacji
/// (wariant / sekcja powtarzana / lista / tabela). Uzupełnia <see cref="DocxTestHelper"/>.
/// </summary>
public static class DocxContentControlTestHelper
{
    public const int BulletNumId = 1;
    public const int DecimalNumId = 2;

    /// <summary>Dokument z podanymi elementami w treści; definicje numeracji (punktory = 1, cyfry = 2) są zawsze dodane.</summary>
    public static MemoryStream CreateDocx(params OpenXmlElement[] bodyChildren)
        => CreateDocx(bodyChildren, headerChildren: null);

    public static MemoryStream CreateDocx(OpenXmlElement[] bodyChildren, OpenXmlElement[]? headerChildren)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            foreach (var child in bodyChildren)
            {
                body.AppendChild(child);
            }

            var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering(
                new AbstractNum(new Level(new NumberingFormat { Val = NumberFormatValues.Bullet }) { LevelIndex = 0 }) { AbstractNumberId = BulletNumId },
                new AbstractNum(new Level(new NumberingFormat { Val = NumberFormatValues.Decimal }) { LevelIndex = 0 }) { AbstractNumberId = DecimalNumId },
                new NumberingInstance(new AbstractNumId { Val = BulletNumId }) { NumberID = BulletNumId },
                new NumberingInstance(new AbstractNumId { Val = DecimalNumId }) { NumberID = DecimalNumId });

            if (headerChildren != null)
            {
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = new Header(headerChildren);
                body.AppendChild(new SectionProperties(new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) }));
            }
        }

        stream.Position = 0;
        return stream;
    }

    public static Paragraph Para(string text) => new(new Run(new Text(text)));

    public static Paragraph ListItem(string text, int numId) => new(
        new ParagraphProperties(new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = numId })),
        new Run(new Text(text)));

    public static Table SimpleTable(params string[] cells) => new(
        new TableRow(cells.Select(c => new TableCell(Para(c))).ToArray<OpenXmlElement>()));

    /// <summary>Formant blokowy (Rich Text) z tagiem i treścią.</summary>
    public static SdtBlock BlockSdt(string tag, params OpenXmlElement[] content)
        => new(new SdtProperties(new Tag { Val = tag }), new SdtContentBlock(content));

    /// <summary>Sekcja powtarzana Worda (w15:repeatingSection) z jednym egzemplarzem (w15:repeatingSectionItem).</summary>
    public static SdtBlock RepeatingSectionSdt(string tag, params OpenXmlElement[] itemContent)
        => new(
            new SdtProperties(new Tag { Val = tag }, new W15.SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(new SdtProperties(new W15.SdtRepeatedSectionItem()), new SdtContentBlock(itemContent))));

    /// <summary>Formant w treści akapitu (poziom run).</summary>
    public static Paragraph ParagraphWithInlineSdt(string before, string tag, string inner)
        => new(new Run(new Text(before) { Space = SpaceProcessingModeValues.Preserve }),
               new SdtRun(new SdtProperties(new Tag { Val = tag }), new SdtContentRun(new Run(new Text(inner)))));

    /// <summary>Formant wiersza tabeli.</summary>
    public static Table TableWithRowSdt(string tag, params string[] cells)
        => new(new SdtRow(new SdtProperties(new Tag { Val = tag }),
                          new SdtContentRow(new TableRow(cells.Select(c => new TableCell(Para(c))).ToArray<OpenXmlElement>()))));

    /// <summary>Pole wyboru (checkbox) — pomijane przez parser.</summary>
    public static SdtBlock CheckBoxSdt(string tag)
        => new(new SdtProperties(new Tag { Val = tag }, new W14.SdtContentCheckBox()), new SdtContentBlock(Para("☐")));
}
