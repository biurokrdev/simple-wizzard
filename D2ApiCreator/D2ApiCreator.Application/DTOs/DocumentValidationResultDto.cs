namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// Wynik walidacji szablonu dokumentu
/// </summary>
public class DocumentValidationResultDto
{
    /// <summary>
    /// Czy dokument jest poprawny
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Lista błędów walidacji
    /// </summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Lista ostrzeżeń
    /// </summary>
    public List<ValidationWarningDto> Warnings { get; set; } = new();

    /// <summary>
    /// Dodatkowe informacje o dokumencie
    /// </summary>
    public DocumentMetadataDto? Metadata { get; set; }
}

/// <summary>
/// Błąd walidacji
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Kod błędu
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Komunikat błędu
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Lokalizacja błędu w dokumencie (opcjonalnie)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Poziom ważności
    /// </summary>
    public string Severity { get; set; } = "Error";
}

/// <summary>
/// Ostrzeżenie walidacji
/// </summary>
public class ValidationWarningDto
{
    /// <summary>
    /// Kod ostrzeżenia
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Komunikat ostrzeżenia
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Lokalizacja w dokumencie (opcjonalnie)
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// Metadane dokumentu
/// </summary>
public class DocumentMetadataDto
{
    /// <summary>
    /// Liczba stron
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Liczba znaków
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Rozmiar pliku w bajtach
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Format dokumentu
    /// </summary>
    public string Format { get; set; } = "DOCX";

    /// <summary>
    /// Czy dokument zawiera makra
    /// </summary>
    public bool ContainsMacros { get; set; }
}
