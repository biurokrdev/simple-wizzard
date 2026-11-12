using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace D2ApiCreator.Tests.Unit.Features.Documents;

/// <summary>
/// Unit tests for ExtractPlaceholdersQueryHandler
/// </summary>
public class ExtractPlaceholdersQueryHandlerTests
{
    private readonly IDocumentPlaceholderService _documentPlaceholderService;
    private readonly ILogger<ExtractPlaceholdersQueryHandler> _logger;
    private readonly ExtractPlaceholdersQueryHandler _sut;

    public ExtractPlaceholdersQueryHandlerTests()
    {
        _documentPlaceholderService = Substitute.For<IDocumentPlaceholderService>();
        _logger = Substitute.For<ILogger<ExtractPlaceholdersQueryHandler>>();
        _sut = new ExtractPlaceholdersQueryHandler(_documentPlaceholderService, Substitute.For<ISpreadsheetTemplateService>(), _logger);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnDto()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "test.docx";
        var placeholders = new List<string> { "<%name%>", "<%email%>" };
        var contentControls = new Dictionary<string, string> { { "tag_test", "boolean" } };

        _documentPlaceholderService
            .ExtractPlaceholdersFromDocxAsync(stream, fileName)
            .Returns(placeholders);
        
        _documentPlaceholderService
            .ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), fileName)
            .Returns(contentControls);

        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = fileName
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Placeholders.Should().BeEquivalentTo(placeholders);
        result.PlaceholdersCount.Should().Be(2);
        result.ContentControls.Should().BeEquivalentTo(contentControls);
        result.ContentControlsCount.Should().Be(1);
        result.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task Handle_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "document.docx";
        var placeholders = new List<string> { "<%test%>" };
        var contentControls = new Dictionary<string, string>();

        _documentPlaceholderService
            .ExtractPlaceholdersFromDocxAsync(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(placeholders);
        
        _documentPlaceholderService
            .ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(contentControls);

        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = fileName
        };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _documentPlaceholderService.Received(1).ExtractPlaceholdersFromDocxAsync(stream, fileName);
        await _documentPlaceholderService.Received(1).ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), fileName);
    }

    [Fact]
    public async Task Handle_WithEmptyPlaceholders_ShouldReturnEmptyDto()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "empty.docx";
        var placeholders = new List<string>();
        var contentControls = new Dictionary<string, string>();

        _documentPlaceholderService
            .ExtractPlaceholdersFromDocxAsync(stream, fileName)
            .Returns(placeholders);
        
        _documentPlaceholderService
            .ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), fileName)
            .Returns(contentControls);

        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = fileName
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Placeholders.Should().BeEmpty();
        result.PlaceholdersCount.Should().Be(0);
        result.ContentControls.Should().NotBeNull();
        result.ContentControlsCount.Should().Be(0);
        result.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task Handle_ShouldLogInformation()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "test.docx";
        var placeholders = new List<string> { "<%test%>" };
        var contentControls = new Dictionary<string, string>();

        _documentPlaceholderService
            .ExtractPlaceholdersFromDocxAsync(stream, fileName)
            .Returns(placeholders);
        
        _documentPlaceholderService
            .ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), fileName)
            .Returns(contentControls);

        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = fileName
        };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Extracting placeholders")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WithMultiplePlaceholders_ShouldReturnCorrectCount()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "multi.docx";
        var placeholders = new List<string> 
        { 
            "<%name%>", 
            "<%email%>", 
            "<%phone%>", 
            "<%address%>", 
            "<%city%>" 
        };
        var contentControls = new Dictionary<string, string>();

        _documentPlaceholderService
            .ExtractPlaceholdersFromDocxAsync(stream, fileName)
            .Returns(placeholders);
        
        _documentPlaceholderService
            .ExtractContentControlsFromDocxAsync(Arg.Any<Stream>(), fileName)
            .Returns(contentControls);

        var query = new ExtractPlaceholdersQuery
        {
            FileStream = stream,
            FileName = fileName
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.PlaceholdersCount.Should().Be(5);
        result.Placeholders.Should().HaveCount(5);
        result.ContentControls.Should().NotBeNull();
    }
}

