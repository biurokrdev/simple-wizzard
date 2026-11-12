using System.Net;
using System.Net.Http.Headers;
using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Tests.Integration.Common;
using D2ApiCreator.Tests.Unit.Helpers;
using FluentAssertions;
using System.Text.Json;

namespace D2ApiCreator.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for DocumentsController
/// </summary>
public class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task ExtractPlaceholders_WithValidDocx_ShouldReturnOk()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("Hello <%name%> world");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "test.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithValidDocx_ShouldReturnCorrectPlaceholders()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("User: <%first_name%> <%last_name%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "test.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().HaveCount(2);
        result.Placeholders.Should().Contain(new[] { "<%first_name%>", "<%last_name%>" });
        result.PlaceholdersCount.Should().Be(2);
        result.ContentControls.Should().NotBeNull();
        result.FileName.Should().Be("test.docx");
    }

    [Fact]
    public async Task ExtractPlaceholders_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "empty.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithNonDocxFile_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(streamContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithPdfFile_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(streamContent, "file", "test.pdf");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithMultiplePlaceholders_ShouldReturnAll()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%p1%> <%p2%> <%p3%> <%p4%> <%p5%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "multi.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.PlaceholdersCount.Should().Be(5);
        result.Placeholders.Should().HaveCount(5);
        result.ContentControls.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractPlaceholders_WithNoPlaceholders_ShouldReturnEmptyList()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("This is plain text without placeholders");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "plain.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().BeEmpty();
        result.PlaceholdersCount.Should().Be(0);
        result.ContentControls.Should().NotBeNull();
        result.ContentControlsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithHeaderAndFooter_ShouldFindAll()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithHeaderAndFooter(
            "Body <%body%>",
            "Header <%header%>",
            "Footer <%footer%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "full.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().HaveCount(3);
        result.Placeholders.Should().Contain(new[] { "<%body%>", "<%header%>", "<%footer%>" });
    }

    [Fact]
    public async Task ExtractPlaceholders_WithDuplicates_ShouldReturnUnique()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%name%> and <%name%> and <%name%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "duplicates.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().HaveCount(1);
        result.PlaceholdersCount.Should().Be(1);
        result.ContentControls.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractPlaceholders_WithComplexPlaceholders_ShouldExtractCorrectly()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%ala_nazwa%> and <%kuta_lubi_cipki%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "complex.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().Contain(new[] { "<%ala_nazwa%>", "<%kuta_lubi_cipki%>" });
    }

    [Fact]
    public async Task ExtractPlaceholders_ResponseShouldBeSorted()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%zebra%> <%alpha%> <%middle%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "sorted.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ExtractPlaceholders_WithMultipleParagraphs_ShouldFindAll()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithParagraphs(
            "First <%p1%>",
            "Second <%p2%>",
            "Third <%p3%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "paragraphs.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithLongFileName_ShouldPreserveFileName()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%test%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        var longFileName = "this_is_a_very_long_file_name_with_many_characters_and_underscores_document.docx";
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", longFileName);

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.FileName.Should().Be(longFileName);
    }

    [Fact]
    public async Task ExtractPlaceholders_ContentTypeShouldBeJson()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%test%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "test.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ExtractPlaceholders_WithUppercaseExtension_ShouldAccept()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%test%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "TEST.DOCX");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExtractPlaceholders_WithSpecialCharactersInPlaceholder_ShouldExtract()
    {
        // Arrange
        using var stream = DocxTestHelper.CreateDocxWithText("<%user.name%> <%user-email%> <%user_phone%>");
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(streamContent, "file", "special.docx");

        // Act
        var response = await _client.PostAsync("/api/documents/extract-placeholders", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentPlaceholderDto>(responseString, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Placeholders.Should().HaveCount(3);
        result.Placeholders.Should().Contain(new[] { "<%user.name%>", "<%user-email%>", "<%user_phone%>" });
    }
}

