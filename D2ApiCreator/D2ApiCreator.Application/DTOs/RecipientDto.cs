namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// DTO reprezentujący odbiorcę dokumentu
/// </summary>
public sealed record RecipientDto
{
    /// <summary>
    /// Unikalny identyfikator odbiorcy
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Kod odbiorcy
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Nazwa odbiorcy
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Opis odbiorcy
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Czy odbiorca jest aktywny
    /// </summary>
    public bool IsActive { get; init; } = true;
}
