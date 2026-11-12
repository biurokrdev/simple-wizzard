namespace D2ApiCreator.Application.DTOs;

/// <summary>
/// DTO reprezentujący tenant
/// </summary>
public sealed record TenantDto
{
    /// <summary>
    /// Unikalny identyfikator tenanta
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Kod tenanta
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Nazwa tenanta
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Opis tenanta
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Czy tenant jest aktywny
    /// </summary>
    public bool IsActive { get; init; } = true;
}
