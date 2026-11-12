namespace D2ApiCreator.Domain.Common.BaseTypes;

/// <summary>
/// Base interface for all Domain Events
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

