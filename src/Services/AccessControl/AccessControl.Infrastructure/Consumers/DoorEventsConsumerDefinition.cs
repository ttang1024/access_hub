using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.AccessControl.Infrastructure.Consumers;

/// <summary>Stale grant detection only looks at grants, so the queue binds to AccessGranted alone.</summary>
public sealed class DoorEventsConsumerDefinition : ConsumerDefinition<DoorEventsConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DoorEventsConsumer> consumerConfigurator, IRegistrationContext context) =>
        endpointConfigurator.BindDoorEvents(DoorEventType.AccessGranted);
}
