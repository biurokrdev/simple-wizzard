using System.Text.Json;

namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;

public class AuditLog : AggregateRoot<Guid>
{
    protected AuditLog() { }

    public AuditLog(Guid id, string entity, Guid? entityId, string action, JsonDocument? payload = null, Guid? actorId = null)
        : base(id)
    {
        Entity = entity;
        EntityId = entityId;
        Action = action;
        Payload = payload;
        ActorId = actorId;
        CreatedAt = DateTime.UtcNow;
    }

    public string Entity { get; private set; } = null!;
    public Guid? EntityId { get; private set; }
    public string Action { get; private set; } = null!;
    public JsonDocument? Payload { get; private set; }
    public Guid? ActorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

