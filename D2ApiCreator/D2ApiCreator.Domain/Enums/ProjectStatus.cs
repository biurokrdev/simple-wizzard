
namespace D2ApiCreator.Domain.Enums;

public enum ProjectStatus
{
    Draft,
    PendingApproval,
    Approved,
    Deploying,
    Active,
    Inactive,
    /// <summary>Odesłana do poprawy przez administratora (BR-017, „Wymaga poprawy”).</summary>
    RequiresChanges
}

