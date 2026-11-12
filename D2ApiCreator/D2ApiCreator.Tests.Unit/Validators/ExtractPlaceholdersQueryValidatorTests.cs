using D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
using D2ApiCreator.Application.Validators;
using FluentAssertions;

namespace D2ApiCreator.Tests.Unit.Validators;

/// <summary>
/// Unit tests for ExtractPlaceholdersQueryValidator
/// </summary>
public class ExtractPlaceholdersQueryValidatorTests
{
    private readonly ExtractPlaceholdersQueryValidator _validator;

    public ExtractPlaceholdersQueryValidatorTests()
    {
        _validator = new ExtractPlaceholdersQueryValidator();
    }

    [Fact]
    public async Task Validate_WithValidDocxFile_ShouldPass()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.docx",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyFileName_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_WithNullFileName_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = null!,
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_WithNonDocxExtension_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName" && e.ErrorMessage.Contains(".docx"));
    }

    [Fact]
    public async Task Validate_WithDocExtension_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.doc",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_WithTxtExtension_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.txt",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNullFileStream_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.docx",
            FileStream = null!
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileStream");
    }

    [Fact]
    public async Task Validate_WithEmptyFileStream_ShouldFail()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.docx",
            FileStream = new MemoryStream()
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileStream" && e.ErrorMessage.Contains("empty"));
    }

    [Fact]
    public async Task Validate_WithUppercaseDocxExtension_ShouldPass()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "DOCUMENT.DOCX",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMixedCaseDocxExtension_ShouldPass()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "document.DocX",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFileNameWithPath_ShouldValidateExtensionOnly()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = @"C:\Documents\MyFolder\document.docx",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var query = new ExtractPlaceholdersQuery
        {
            FileName = "",
            FileStream = null!
        };

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }
}

