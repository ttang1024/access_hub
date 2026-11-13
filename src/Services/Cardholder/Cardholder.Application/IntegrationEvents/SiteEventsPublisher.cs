using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Domain.Sites;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.Application.IntegrationEvents;

/// <summary>Maps site domain events to integration events, added to the outbox before commit.</summary>
public sealed class SiteEventsPublisher(IIntegrationEventPublisher publisher)
    : IDomainEventHandler<SiteRegisteredDomainEvent>, IDomainEventHandler<SiteApiKeyRotatedDomainEvent>
{
    public Task HandleAsync(SiteRegisteredDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new SiteRegistered(Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.SiteId.Value, e.Name, e.TimeZoneId, Convert.ToBase64String(e.ApiKeyHash), e.Version),
            cancellationToken);

    public Task HandleAsync(SiteApiKeyRotatedDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new SiteApiKeyRotated(Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.SiteId.Value, e.Name, e.TimeZoneId, Convert.ToBase64String(e.ApiKeyHash), e.Version),
            cancellationToken);
}
