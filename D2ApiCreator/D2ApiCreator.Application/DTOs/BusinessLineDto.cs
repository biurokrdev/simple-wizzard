namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// DTO reprezentujący linię biznesową
/// </summary>
public sealed record BusinessLineDto
{
    /// <summary>
    /// Unikalny identyfikator linii biznesowej
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Kod linii biznesowej
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Nazwa linii biznesowej
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Opis linii biznesowej
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Czy linia biznesowa jest aktywna
    /// </summary>
    public bool IsActive { get; init; } = true;
}
