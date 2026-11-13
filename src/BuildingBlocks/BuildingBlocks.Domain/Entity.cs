namespace AccessHub.BuildingBlocks.Domain;

/// <summary>An object with identity. Two entities are equal when their IDs are equal.</summary>
/// <typeparam name="TId">The strongly typed ID.</typeparam>
public abstract class Entity<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>For EF Core materialisation.</summary>
    protected Entity()
    {
    }

    /// <summary>Creates an entity with the given ID.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>The identity.</summary>
    public TId Id { get; private init; }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && other.GetType() == GetType() && other.Id.Equals(Id);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();
}
