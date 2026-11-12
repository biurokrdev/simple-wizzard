namespace D2ApiCreator.Domain.ValueObjects;

using Common.BaseTypes;

public class ProjectMetadata : ValueObject
{
    public string? ApplicationName { get; init; }
    public string? ProcessName { get; init; }
    public string? ProcessOwner { get; init; }
    public string? BusinessLineId { get; init; }
    public string? ProcessVersion { get; init; }
    public string? ProcessStep { get; init; }
    public string? StatusId { get; init; }
    public string? RecipientId { get; init; }
    /// <summary>Login (corporate key) autora — decyduje o widoczności projektu prywatnego (bez zespołu).</summary>
    public string? OwnerCorporateKey { get; init; }

    public ProjectMetadata() { }

    public ProjectMetadata(
        string? applicationName = null,
        string? processName = null,
        string? processOwner = null,
        string? businessLineId = null,
        string? processVersion = null,
        string? processStep = null,
        string? statusId = null,
        string? recipientId = null,
        string? ownerCorporateKey = null)
    {
        ApplicationName = applicationName;
        ProcessName = processName;
        ProcessOwner = processOwner;
        BusinessLineId = businessLineId;
        ProcessVersion = processVersion;
        ProcessStep = processStep;
        StatusId = statusId;
        RecipientId = recipientId;
        OwnerCorporateKey = ownerCorporateKey;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ApplicationName;
        yield return ProcessName;
        yield return ProcessOwner;
        yield return BusinessLineId;
        yield return ProcessVersion;
        yield return ProcessStep;
        yield return StatusId;
        yield return RecipientId;
        yield return OwnerCorporateKey;
    }
}
