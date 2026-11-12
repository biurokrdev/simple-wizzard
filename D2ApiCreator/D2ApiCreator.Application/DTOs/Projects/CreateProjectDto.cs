namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO for creating a new project
/// </summary>
/// <remarks>
/// Metadata fields (ApplicationName, ProcessName, ProcessOwner, etc.) are stored in a single JSONB column in the database.
/// </remarks>
public class CreateProjectDto
{
    /// <summary>
    /// Tenant identifier (GUID or name of the team/group) - OPTIONAL
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Source identifier (auto-generated if not provided)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Project name (documentName from wizard)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Application/system name
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Recipient identifier
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// Retention days
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Target environments (DEV, UAT, PREPROD, PROD)
    /// </summary>
    public List<string> Environments { get; set; } = new();

    /// <summary>
    /// Business process name
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Process owner name
    /// </summary>
    public string? ProcessOwner { get; set; }

    /// <summary>
    /// Business line identifier
    /// </summary>
    public string? BusinessLineId { get; set; }

    /// <summary>
    /// Process version
    /// </summary>
    public string? ProcessVersion { get; set; }

    /// <summary>
    /// Process step
    /// </summary>
    public string? ProcessStep { get; set; }

    /// <summary>
    /// Status identifier
    /// </summary>
    public string? StatusId { get; set; }

    /// <summary>
    /// Project description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User ID creating the project
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Login (corporate key) autora — właściciel projektu prywatnego; do czasu R-03 podawany przez GUI.</summary>
    public string? OwnerCorporateKey { get; set; }
}
