using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Consumers;

public sealed class LiveFeedConsumerDefinition : ConsumerDefinition<LiveFeedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<LiveFeedConsumer> consumerConfigurator, IRegistrationContext context) =>
        endpointConfigurator.BindDoorEvents(Enum.GetValues<DoorEventType>());
}
