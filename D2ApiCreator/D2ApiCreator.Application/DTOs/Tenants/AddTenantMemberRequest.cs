namespace D2ApiCreator.Application.DTOs.Tenants;

/// <summary>
/// Request to add a member to tenant
/// </summary>
public class AddTenantMemberRequest
{
    public string CorporateKey { get; set; } = string.Empty;
}
