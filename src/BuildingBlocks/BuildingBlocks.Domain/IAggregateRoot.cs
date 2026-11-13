namespace AccessHub.BuildingBlocks.Domain;

/// <summary>Non-generic view of an aggregate root, used by the unit of work to collect domain events.</summary>
public interface IAggregateRoot
{
    /// <summary>Increments on every change. Carried on integration events so consumers can ignore older versions.</summary>
    int Version { get; }

    /// <summary>Domain events raised since the aggregate was loaded.</summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears collected domain events after they are dispatched.</summary>
    void ClearDomainEvents();
}
