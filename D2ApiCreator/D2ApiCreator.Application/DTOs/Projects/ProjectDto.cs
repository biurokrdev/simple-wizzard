namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO representing a project
/// </summary>
/// <remarks>
/// Metadata fields (ApplicationName, ProcessName, ProcessOwner, etc.) are stored in a single JSONB column in the database.
/// </remarks>
public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    /// <summary>Nazwa zespołu (tenanta) — do list i szczegółów bez dodatkowego zapytania; null dla projektu prywatnego.</summary>
    public string? TenantName { get; set; }
    /// <summary>Login autora projektu prywatnego (metadata.OwnerCorporateKey).</summary>
    public string? OwnerCorporateKey { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ApplicationName { get; set; }
    public string? RecipientId { get; set; }
    public int RetentionDays { get; set; }
    public string? ProcessName { get; set; }
    public string? ProcessOwner { get; set; }
    public string? BusinessLineId { get; set; }
    public string? ProcessVersion { get; set; }
    public string? ProcessStep { get; set; }
    public string? StatusId { get; set; }
    public string? DefaultTemplateName { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public Guid? CurrentActiveVersionId { get; set; }
    
    // Fields from active version
    public string? ActiveVersionTag { get; set; }
    public string? ActiveVersionTemplateName { get; set; }

    /// <summary>
    /// Template shown on lists: file of the active version, otherwise of the newest draft version,
    /// otherwise <see cref="DefaultTemplateName"/>. Lets editors tell parallel drafts apart before activation.
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>Version tag the <see cref="TemplateName"/> comes from (null when no version exists).</summary>
    public string? TemplateVersionTag { get; set; }

    /// <summary>True when <see cref="TemplateName"/> comes from the active version; false for a draft.</summary>
    public bool TemplateVersionIsActive { get; set; }
}
