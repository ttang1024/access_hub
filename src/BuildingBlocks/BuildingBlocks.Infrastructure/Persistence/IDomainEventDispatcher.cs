using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Dispatches domain events to their <c>IDomainEventHandler</c>s.</summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches one event to every registered handler.</summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
