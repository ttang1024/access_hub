namespace AccessHub.BuildingBlocks.Application;

/// <summary>Publishes integration events through the transactional outbox. Never publishes directly to the bus.</summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Adds the event to the outbox of the current transaction.</summary>
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken)
        where T : class;
}
