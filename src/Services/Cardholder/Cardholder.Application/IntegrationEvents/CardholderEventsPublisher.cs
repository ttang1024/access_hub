using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.Application.IntegrationEvents;

public sealed class CardholderEventsPublisher(IIntegrationEventPublisher publisher) : IDomainEventHandler<CardholderChangedDomainEvent>
{
    public Task HandleAsync(CardholderChangedDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new CardholderChanged(
                Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(),
                e.CardholderId.Value, e.FirstName, e.LastName, e.IsActive, e.AccessGroupIds.Select(g => g.Value).ToList(), e.Version),
            cancellationToken);
}
