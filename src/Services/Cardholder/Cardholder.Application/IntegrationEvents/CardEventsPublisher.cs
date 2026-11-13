using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Domain.Cards;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.Application.IntegrationEvents;

public sealed class CardEventsPublisher(IIntegrationEventPublisher publisher)
    : IDomainEventHandler<CardIssuedDomainEvent>, IDomainEventHandler<CardRevokedDomainEvent>
{
    public Task HandleAsync(CardIssuedDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new CardIssued(Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.CardId.Value, e.CardholderId.Value, e.CardNumber, e.ExpiresAt, false, null, e.Version),
            cancellationToken);

    public Task HandleAsync(CardRevokedDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new CardRevoked(Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.CardId.Value, e.CardholderId.Value, e.CardNumber, e.ExpiresAt, true, e.RevokedAt, e.Version),
            cancellationToken);
}
