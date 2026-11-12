using System.Text.Json;

namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;
using Enums;

public class Mapping : AggregateRoot<Guid>
{
    protected Mapping() { }

    public Mapping(Guid id, Guid projectVersionId, string tagName, MappingType mappingType, JsonDocument mappingJson, int sortOrder = 100, Guid? createdBy = null)
        : base(id)
    {
        ProjectVersionId = projectVersionId;
        TagName = tagName;
        MappingType = mappingType;
        MappingJson = mappingJson;
        SortOrder = sortOrder;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid ProjectVersionId { get; private set; }
    public string TagName { get; private set; } = null!;
    public MappingType MappingType { get; private set; }
    public JsonDocument MappingJson { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public ProjectVersion ProjectVersion { get; private set; } = null!;
    public User? Creator { get; private set; }

    public void UpdateMapping(MappingType mappingType, JsonDocument mappingJson)
    {
        MappingType = mappingType;
        MappingJson = mappingJson;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}

