using System.Collections.Concurrent;
using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.Testing;

/// <summary>
/// A separate bus bound to the real broker. It proves events went through the outbox and reached RabbitMQ,
/// rather than asserting on an in-process fake.
/// </summary>
public sealed class PublishedEventsListener : IAsyncDisposable
{
    private readonly ConcurrentQueue<object> _messages = new();
    private IBusControl? _bus;

    public static async Task<PublishedEventsListener> StartAsync(string rabbitMqUri, IEnumerable<Type> messageTypes, CancellationToken cancellationToken)
    {
        var listener = new PublishedEventsListener();
        var types = messageTypes.ToList();
        listener._bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(new Uri(rabbitMqUri));
            cfg.ConfigureDoorEventTopology();
            cfg.ReceiveEndpoint($"test-listener-{Guid.NewGuid():N}", (IRabbitMqReceiveEndpointConfigurator e) =>
            {
                e.AutoDelete = true;
                e.Durable = false;
                e.ConfigureConsumeTopology = false;
                foreach (var type in types)
                {
                    if (type == typeof(DoorEventsRecorded))
                    {
                        e.BindDoorEvents(Enum.GetValues<DoorEventType>());
                    }
                    else
                    {
                        // MassTransit's default exchange name for a message type.
                        e.Bind($"{type.Namespace}:{type.Name}");
                    }
                }

                foreach (var type in types)
                {
                    typeof(PublishedEventsListener).GetMethod(nameof(AddHandler), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .MakeGenericMethod(type).Invoke(listener, [e]);
                }
            });
        });
        await listener._bus.StartAsync(cancellationToken);
        return listener;
    }

    public IReadOnlyList<T> All<T>() => _messages.OfType<T>().ToList();

    /// <summary>Publishes one stored site event the way ingestion does: as a one-event <see cref="DoorEventsRecorded"/> batch.</summary>
    public Task PublishStoredEventAsync(DoorEventRecorded e, Guid? messageId = null) =>
        PublishAsync(
            new DoorEventsRecorded(e.EventId, e.RecordedAt, e.CorrelationId, e.SiteId, e.Type,
                [new RecordedDoorEvent(e.SourceEventId, e.DoorId, e.OccurredAt, e.CardNumber, e.DenyReason, e.AccessListSequence, e.RecordedAt)]),
            messageId);

    /// <summary>Every stored site event seen so far, flattened out of the batches.</summary>
    public IReadOnlyList<DoorEventRecorded> StoredEvents() => All<DoorEventsRecorded>().SelectMany(b => b.Expand()).ToList();

    /// <summary>Publishes to the real broker, optionally reusing a message ID to simulate redelivery.</summary>
    public Task PublishAsync<T>(T message, Guid? messageId = null)
        where T : class =>
        _bus!.Publish(message, ctx =>
        {
            if (messageId is { } id)
            {
                ctx.MessageId = id;
            }
        });

    public async Task<T> WaitForAsync<T>(Func<T, bool> predicate, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (!cts.IsCancellationRequested)
        {
            var match = _messages.OfType<T>().FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(50, CancellationToken.None);
        }

        throw new TimeoutException($"No {typeof(T).Name} matching the predicate was published. Seen: {string.Join(", ", _messages.Select(m => m.GetType().Name))}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_bus is not null)
        {
            await _bus.StopAsync();
        }
    }

    private void AddHandler<T>(IRabbitMqReceiveEndpointConfigurator endpoint)
        where T : class =>
        endpoint.Handler<T>(ctx =>
        {
            _messages.Enqueue(ctx.Message);
            return Task.CompletedTask;
        });
}
