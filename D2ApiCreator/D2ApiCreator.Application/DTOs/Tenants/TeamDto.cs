namespace D2ApiCreator.Application.DTOs.Tenants;

/// <summary>
/// DTO for team information
/// </summary>
public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public List<TenantMemberDto> Members { get; set; } = new();
    /// <summary>`false` = zespół zablokowany przez administratora (BR-018).</summary>
    public bool IsActive { get; set; } = true;
}
