using D2ApiCreator.Infrastructure.Services;
using D2ApiCreator.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Services;

/// <summary>
/// Unit tests for DocumentPlaceholderService
/// </summary>
public class DocumentPlaceholderServiceTests
{
    private readonly ILogger<DocumentPlaceholderService> _logger;
    private readonly DocumentPlaceholderService _sut;

    public DocumentPlaceholderServiceTests()
    {
        _logger = Substitute.For<ILogger<DocumentPlaceholderService>>();
        _sut = new DocumentPlaceholderService(_logger);
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithSinglePlaceholder_ShouldReturnOnePlaceholder()
    {
        // Arrange
        var text = "Hello <%name%> world";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("<%name%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithMultiplePlaceholders_ShouldReturnAllPlaceholders()
    {
        // Arrange
        var text = "User: <%first_name%> <%last_name%>, Email: <%email%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%first_name%>", "<%last_name%>", "<%email%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithDuplicatePlaceholders_ShouldReturnUniqueOnly()
    {
        // Arrange
        var text = "<%name%> and <%name%> again <%name%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("<%name%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithNoPlaceholders_ShouldReturnEmptyList()
    {
        // Arrange
        var text = "This is just plain text without any placeholders";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithEmptyDocument_ShouldReturnEmptyList()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateEmptyDocx();
        var fileName = "empty.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithPlaceholdersInMultipleParagraphs_ShouldFindAll()
    {
        // Arrange
        var paragraphs = new[]
        {
            "First paragraph with <%placeholder1%>",
            "Second paragraph with <%placeholder2%>",
            "Third paragraph with <%placeholder3%>"
        };
        using var stream = DocxTestHelper.CreateDocxWithParagraphs(paragraphs);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%placeholder1%>", "<%placeholder2%>", "<%placeholder3%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithPlaceholdersInHeader_ShouldFindThem()
    {
        // Arrange
        var bodyText = "Body with <%body_placeholder%>";
        var headerText = "Header with <%header_placeholder%>";
        var footerText = "Footer text";
        using var stream = DocxTestHelper.CreateDocxWithHeaderAndFooter(bodyText, headerText, footerText);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "<%body_placeholder%>", "<%header_placeholder%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithPlaceholdersInFooter_ShouldFindThem()
    {
        // Arrange
        var bodyText = "Body text";
        var headerText = "Header text";
        var footerText = "Footer with <%footer_placeholder%>";
        using var stream = DocxTestHelper.CreateDocxWithHeaderAndFooter(bodyText, headerText, footerText);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("<%footer_placeholder%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithPlaceholdersEverywhere_ShouldFindAll()
    {
        // Arrange
        var bodyText = "Body <%body%>";
        var headerText = "Header <%header%>";
        var footerText = "Footer <%footer%>";
        using var stream = DocxTestHelper.CreateDocxWithHeaderAndFooter(bodyText, headerText, footerText);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%body%>", "<%header%>", "<%footer%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithUnderscoresInPlaceholder_ShouldExtractCorrectly()
    {
        // Arrange
        var text = "User: <%first_name%> <%last_name%> <%email_address%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%first_name%>", "<%last_name%>", "<%email_address%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithNumbersInPlaceholder_ShouldExtractCorrectly()
    {
        // Arrange
        var text = "Values: <%value1%> <%value2%> <%value123%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%value1%>", "<%value2%>", "<%value123%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithSpecialExample_ShouldExtractCorrectly()
    {
        // Arrange
        var text = "Examples: <%ala_nazwa%> and <%kuta_lubi_cipki%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "<%ala_nazwa%>", "<%kuta_lubi_cipki%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithIncompletePlaceholder_ShouldNotExtract()
    {
        // Arrange
        var text = "Incomplete: <%incomplete_or_missing_end%> or <%missing_start";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        // Should only extract the one complete placeholder
        result.Should().HaveCount(1);
        result.Should().Contain("<%incomplete_or_missing_end%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithNestedBrackets_ShouldExtractOuterOnly()
    {
        // Arrange
        var text = "Test <%outer%> normal";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("<%outer%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_ShouldReturnSortedList()
    {
        // Arrange
        var text = "<%zebra%> <%alpha%> <%middle%> <%beta%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(4);
        result.Should().BeInAscendingOrder();
        result[0].Should().Be("<%alpha%>");
        result[1].Should().Be("<%beta%>");
        result[2].Should().Be("<%middle%>");
        result[3].Should().Be("<%zebra%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithWhitespaceInPlaceholder_ShouldNotExtract()
    {
        // Arrange
        var text = "Test <%name with spaces%> here";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - placeholders with spaces should be ignored
        result.Should().BeEmpty();
        
        // Should log a warning
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("whitespace")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithMixedCase_ShouldPreserveCase()
    {
        // Arrange
        var text = "<%FirstName%> <%LASTNAME%> <%eMaIl%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "<%FirstName%>", "<%LASTNAME%>", "<%eMaIl%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithAdjacentPlaceholders_ShouldExtractBoth()
    {
        // Arrange
        var text = "<%first%><%second%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "<%first%>", "<%second%>" });
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithHyphensInPlaceholder_ShouldNotExtract()
    {
        // Arrange
        var text = "<%first-name%> <%last-name%> <%e-mail-address%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - hyphens are not allowed in placeholders
        result.Should().BeEmpty();
        
        // Should log warnings for invalid characters
        _logger.Received(3).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("invalid characters")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithLongPlaceholderName_ShouldExtract()
    {
        // Arrange
        var text = "<%this_is_a_very_long_placeholder_name_with_many_underscores_and_words%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("<%this_is_a_very_long_placeholder_name_with_many_underscores_and_words%>");
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_ShouldLogInformation()
    {
        // Arrange
        var text = "Test <%placeholder%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Extracted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithInvalidStream_ShouldThrowAndLog()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }); // Invalid DOCX
        var fileName = "invalid.docx";

        // Act
        var act = async () => await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithDotInPlaceholder_ShouldNotExtract()
    {
        // Arrange
        var text = "<%user.name%> <%user.email%> <%company.address.street%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - dots are not allowed in placeholders
        result.Should().BeEmpty();
        
        // Should log warnings for invalid characters
        _logger.Received(3).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("invalid characters")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithEmptyPlaceholder_ShouldNotExtractAndLogWarning()
    {
        // Arrange
        var text = "Empty placeholder: <%%> should be ignored";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - empty placeholder should be ignored
        result.Should().BeEmpty();
        
        // Should log a warning about empty placeholder
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("empty placeholder")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithSpacedPlaceholder_ShouldNotExtractAndLogWarning()
    {
        // Arrange - case from user's document: <% lubie_frytki %>
        var text = "Spaced placeholder: <% lubie_frytki %> should be ignored";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - placeholder with spaces around content should be ignored
        result.Should().BeEmpty();
        
        // Should log a warning about whitespace
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("whitespace")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExtractPlaceholdersFromDocxAsync_WithValidAndInvalidPlaceholders_ShouldExtractOnlyValid()
    {
        // Arrange - mix of valid and invalid placeholders
        var text = "Valid: <%data_aktualna%> <%porto%> Invalid: <%%> <% lubie_frytki %> <%first-name%>";
        using var stream = DocxTestHelper.CreateDocxWithText(text);
        var fileName = "test.docx";

        // Act
        var result = await _sut.ExtractPlaceholdersFromDocxAsync(stream, fileName);

        // Assert - only valid placeholders should be extracted
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "<%data_aktualna%>", "<%porto%>" });
        
        // Should log warnings for invalid ones
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("empty placeholder") || 
                               o.ToString()!.Contains("whitespace") || 
                               o.ToString()!.Contains("invalid characters")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}

