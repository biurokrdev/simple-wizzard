namespace D2ApiCreator.Application.DTOs.Tenants;

/// <summary>
/// Request to update tenant information
/// </summary>
public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
