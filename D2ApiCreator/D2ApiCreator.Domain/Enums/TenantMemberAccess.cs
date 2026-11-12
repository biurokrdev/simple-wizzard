namespace D2ApiCreator.Domain.Enums;

/// <summary>Poziom dostępu członka zespołu do projektów zespołu (BR-018).</summary>
public enum TenantMemberAccess
{
    /// <summary>Tworzy i edytuje projekty zespołu.</summary>
    ReadWrite,

    /// <summary>Tylko podgląd projektów zespołu.</summary>
    ReadOnly
}
