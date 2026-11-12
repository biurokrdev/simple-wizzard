namespace D2ApiCreator.Application.DTOs.Tenants;

/// <summary>
/// Request to create a new tenant/team
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> MemberCorporateKeys { get; set; } = new();
}
