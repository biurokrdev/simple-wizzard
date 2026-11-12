namespace D2ApiCreator.Application.DTOs.Tenants;

/// <summary>
/// DTO for tenant member information
/// </summary>
public class TenantMemberDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CorporateKey { get; set; } = string.Empty;
    public Guid AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
    /// <summary>Zablokowany w zespole (BR-018).</summary>
    public bool IsBlocked { get; set; }
    /// <summary>"ReadWrite" | "ReadOnly" (BR-018).</summary>
    public string Access { get; set; } = "ReadWrite";
}
