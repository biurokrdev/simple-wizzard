using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace D2ApiCreator.Tests.Unit.Helpers;

/// <summary>
/// Helper class for creating test DOCX documents
/// </summary>
public static class DocxTestHelper
{
    /// <summary>
    /// Creates a simple DOCX document with the specified text content
    /// </summary>
    public static MemoryStream CreateDocxWithText(string text)
    {
        var stream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(text));
        }
        
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a DOCX document with multiple paragraphs
    /// </summary>
    public static MemoryStream CreateDocxWithParagraphs(params string[] paragraphs)
    {
        var stream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            foreach (var paragraphText in paragraphs)
            {
                var paragraph = body.AppendChild(new Paragraph());
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(new Text(paragraphText));
            }
        }
        
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a DOCX document with header and footer
    /// </summary>
    public static MemoryStream CreateDocxWithHeaderAndFooter(string bodyText, string headerText, string footerText)
    {
        var stream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Add body content
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(bodyText));

            // Add header
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header();
            var headerParagraph = headerPart.Header.AppendChild(new Paragraph());
            var headerRun = headerParagraph.AppendChild(new Run());
            headerRun.AppendChild(new Text(headerText));

            // Add footer
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer();
            var footerParagraph = footerPart.Footer.AppendChild(new Paragraph());
            var footerRun = footerParagraph.AppendChild(new Run());
            footerRun.AppendChild(new Text(footerText));

            // Link header and footer to document
            var sectionProperties = body.AppendChild(new SectionProperties());
            
            var headerReference = new HeaderReference()
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(headerPart)
            };
            sectionProperties.Append(headerReference);

            var footerReference = new FooterReference()
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(footerPart)
            };
            sectionProperties.Append(footerReference);
        }
        
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates an empty DOCX document
    /// </summary>
    public static MemoryStream CreateEmptyDocx()
    {
        var stream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.AppendChild(new Body());
        }
        
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a DOCX document with text split across multiple runs (to test fragmented placeholders)
    /// </summary>
    public static MemoryStream CreateDocxWithFragmentedText(params string[] textFragments)
    {
        var stream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            var paragraph = body.AppendChild(new Paragraph());

            foreach (var fragment in textFragments)
            {
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(new Text(fragment));
            }
        }
        
        stream.Position = 0;
        return stream;
    }
}

