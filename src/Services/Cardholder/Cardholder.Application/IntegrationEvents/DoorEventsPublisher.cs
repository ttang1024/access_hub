using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.Application.IntegrationEvents;

public sealed class DoorEventsPublisher(IIntegrationEventPublisher publisher) : IDomainEventHandler<DoorRegisteredDomainEvent>
{
    public Task HandleAsync(DoorRegisteredDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new DoorRegistered(Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.DoorId.Value, e.SiteId.Value, e.Name, e.Version),
            cancellationToken);
}
