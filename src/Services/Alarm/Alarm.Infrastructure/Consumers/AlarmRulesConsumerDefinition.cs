using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Consumers;

/// <summary>The rules only fire on these event types; grants, opens and closes never reach this queue.</summary>
public sealed class AlarmRulesConsumerDefinition : ConsumerDefinition<AlarmRulesConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<AlarmRulesConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.BindDoorEvents(DoorEventType.DoorForced, DoorEventType.DoorHeldOpen, DoorEventType.AccessDenied);
        endpointConfigurator.BindFanout<StaleAccessGranted>();
    }
}
