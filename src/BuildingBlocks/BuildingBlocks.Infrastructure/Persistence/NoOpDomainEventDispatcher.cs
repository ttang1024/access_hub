using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Used only by design-time DbContext factories (migrations), where no events are raised.</summary>
public sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
