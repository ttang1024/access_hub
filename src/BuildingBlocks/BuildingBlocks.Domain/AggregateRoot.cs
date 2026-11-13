namespace AccessHub.BuildingBlocks.Domain;

/// <summary>Base for aggregate roots: tracks a version and collects domain events.</summary>
/// <typeparam name="TId">The strongly typed ID.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>For EF Core materialisation.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>Creates an aggregate with the given ID.</summary>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <inheritdoc />
    public int Version { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Records a change: increments <see cref="Version"/> and raises the event built from the new version.</summary>
    /// <param name="createEvent">Builds the event from the new version, so the event and the aggregate always agree.</param>
    protected void Apply(Func<int, IDomainEvent> createEvent)
    {
        Version++;
        _domainEvents.Add(createEvent(Version));
    }
}
