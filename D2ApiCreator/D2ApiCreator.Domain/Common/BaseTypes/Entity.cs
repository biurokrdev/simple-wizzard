namespace D2ApiCreator.Domain.Common.BaseTypes;

/// <summary>
/// Base class for all entities with strongly-typed ID
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity()
    {
        // Required for EF Core
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        // Id is set by EF Core after save, so it's technically mutable.
        // However, once set, it should never change, making it suitable for GetHashCode.
        // This is a standard pattern in DDD with EF Core.
#pragma warning disable CA1065 // Do not raise exceptions in GetHashCode
        return HashCode.Combine(GetType(), Id);
#pragma warning restore CA1065
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}

