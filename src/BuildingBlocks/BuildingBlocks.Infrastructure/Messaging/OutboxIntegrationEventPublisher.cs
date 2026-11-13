using AccessHub.BuildingBlocks.Application;
using MassTransit;

namespace AccessHub.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Publishes through MassTransit's scoped publish endpoint. With the EF bus outbox configured, that endpoint writes
/// to the outbox tables of the current DbContext instead of the broker, so the message commits with the state change.
/// </summary>
public sealed class OutboxIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken)
        where T : class => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
