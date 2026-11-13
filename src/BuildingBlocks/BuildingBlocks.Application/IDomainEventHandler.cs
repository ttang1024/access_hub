using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Reacts to a domain event inside the unit of work, typically by mapping it to an integration event.</summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Handles the event before the transaction commits.</summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
