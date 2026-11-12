namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// DTO reprezentujący status dokumentu
/// </summary>
public sealed record StatusDto
{
    /// <summary>
    /// Unikalny identyfikator statusu
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Kod statusu
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Nazwa statusu
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Opis statusu
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Czy status jest aktywny
    /// </summary>
    public bool IsActive { get; init; } = true;
}
