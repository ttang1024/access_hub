using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;
using RabbitMQ.Client;

namespace AccessHub.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// <see cref="DoorEventsRecorded"/> is the highest-volume event. Each message holds one event type and is published to
/// a topic exchange with that type as routing key, so each consumer's queue receives only the types it handles: a batch
/// a consumer would ignore never costs it an inbox transaction (ADR 0010, ADR 0011).
/// </summary>
public static class DoorEventRouting
{
    /// <summary>Publishes <see cref="DoorEventsRecorded"/> to a topic exchange keyed by event type. Every bus declares it the same way.</summary>
    public static void ConfigureDoorEventTopology(this IRabbitMqBusFactoryConfigurator cfg)
    {
        cfg.Publish<DoorEventsRecorded>(p => p.ExchangeType = ExchangeType.Topic);
        cfg.Send<DoorEventsRecorded>(s => s.UseRoutingKeyFormatter(c => c.Message.Type.ToString()));
    }

    /// <summary>
    /// Binds the endpoint to the given event types only. Consumers still check the type themselves, so a broad binding
    /// (or the in-memory test transport, which has no routing) is never incorrect, only slower.
    /// </summary>
    public static void BindDoorEvents(this IReceiveEndpointConfigurator endpoint, params DoorEventType[] types)
    {
        if (endpoint is not IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            return;
        }

        rabbit.ConfigureConsumeTopology = false;
        foreach (var type in types)
        {
            rabbit.Bind<DoorEventsRecorded>(b =>
            {
                b.ExchangeType = ExchangeType.Topic;
                b.RoutingKey = type.ToString();
            });
        }
    }

    /// <summary>Binds a message type with the default (fanout) topology, for endpoints that disabled consume topology.</summary>
    public static void BindFanout<T>(this IReceiveEndpointConfigurator endpoint)
        where T : class
    {
        if (endpoint is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.Bind<T>();
        }
    }
}
