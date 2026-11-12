using D2ApiCreator.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Interfejs serwisu do walidacji dokumentów Word
/// </summary>
public interface IDocumentValidationService
{
    /// <summary>
    /// Waliduje szablon dokumentu Word
    /// </summary>
    /// <param name="fileStream">Strumień pliku DOCX</param>
    /// <param name="fileName">Nazwa pliku</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Wynik walidacji</returns>
    Task<DocumentValidationResultDto> ValidateDocumentTemplateAsync(
        Stream fileStream, 
        string fileName, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Mock implementacja serwisu walidacji dokumentów
/// Docelowo będzie komunikować się z zewnętrznym serwisem walidacji
/// </summary>
public sealed class MockDocumentValidationService : IDocumentValidationService
{
    private readonly ILogger<MockDocumentValidationService> _logger;

    public MockDocumentValidationService(ILogger<MockDocumentValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentValidationResultDto> ValidateDocumentTemplateAsync(
        Stream fileStream, 
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating document template: {FileName}", fileName);

        // Symulacja opóźnienia sieciowego (wywołanie zewnętrznego API)
        await Task.Delay(500, cancellationToken);

        // Sprawdzenie podstawowych parametrów
        if (fileStream == null || fileStream.Length == 0)
        {
            return new DocumentValidationResultDto
            {
                IsValid = false,
                Errors = new List<ValidationErrorDto>
                {
                    new ValidationErrorDto
                    {
                        Code = "EMPTY_FILE",
                        Message = "Plik jest pusty",
                        Severity = "Error"
                    }
                }
            };
        }

        // Mock - losowo symuluj błędy lub sukces (70% sukces, 30% błędy)
        var random = new Random();
        var hasErrors = random.Next(100) < 30; // 30% szans na błędy
        
        DocumentValidationResultDto result;
        
        if (!hasErrors)
        {
            // Dokument jest OK
            result = new DocumentValidationResultDto
            {
                IsValid = true,
                Errors = new List<ValidationErrorDto>(),
                Warnings = new List<ValidationWarningDto>(),
                Metadata = new DocumentMetadataDto
                {
                    FileSizeBytes = fileStream.Length,
                    Format = "DOCX",
                    PageCount = random.Next(1, 5),
                    CharacterCount = random.Next(500, 3000),
                    ContainsMacros = false
                }
            };

            _logger.LogInformation(
                "Document validation completed: {FileName}, IsValid: {IsValid}", 
                fileName, 
                result.IsValid);
        }
        else
        {
            // Mock - symulacja znalezienia błędów i ostrzeżeń
            var errors = new List<ValidationErrorDto>
            {
                new ValidationErrorDto
                {
                    Code = "INVALID_PLACEHOLDER",
                    Message = "Nieprawidłowy format placeholdera - brakuje zamykającego znacznika %>",
                    Location = "Strona 1, akapit 3",
                    Severity = "Error"
                },
                new ValidationErrorDto
                {
                    Code = "MISSING_CLOSING_TAG",
                    Message = "Content control 'dane_klienta' nie ma zamykającego tagu",
                    Location = "Strona 2, sekcja 1",
                    Severity = "Error"
                }
            };

            var warnings = new List<ValidationWarningDto>
            {
                new ValidationWarningDto
                {
                    Code = "DEPRECATED_SYNTAX",
                    Message = "Placeholder używa starej składni {zmienna} zamiast <%zmienna%>",
                    Location = "Strona 1, akapit 5"
                },
                new ValidationWarningDto
                {
                    Code = "UNKNOWN_CONTENT_CONTROL",
                    Message = "Content control typu 'custom-type' może nie być poprawnie obsługiwany",
                    Location = "Strona 3, akapit 2"
                }
            };

            result = new DocumentValidationResultDto
            {
                IsValid = false, // Dokument ma błędy
                Errors = errors,
                Warnings = warnings,
                Metadata = new DocumentMetadataDto
                {
                    FileSizeBytes = fileStream.Length,
                    Format = "DOCX",
                    PageCount = random.Next(1, 5),
                    CharacterCount = random.Next(500, 3000),
                    ContainsMacros = random.Next(100) < 10 // 10% szans na makra
                }
            };

            _logger.LogInformation(
                "Document validation completed: {FileName}, IsValid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
                fileName, 
                result.IsValid,
                result.Errors.Count,
                result.Warnings.Count);
        }

        return result;
    }
}
