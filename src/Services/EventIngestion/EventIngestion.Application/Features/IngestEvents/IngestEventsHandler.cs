using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Domain.Events;

namespace AccessHub.EventIngestion.Application.Features.IngestEvents;

/// <summary>
/// Stores new events and, in the same transaction, publishes exactly those through the outbox: one
/// <see cref="DoorEventsRecorded"/> per event type in the batch (ADR 0011). A replayed batch stores and publishes nothing.
/// </summary>
public sealed class IngestEventsHandler(IDoorEventStore store, IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher, IngestionMetrics metrics, TimeProvider clock)
    : ICommandHandler<IngestEventsCommand, IngestEventsResponse>
{
    public async Task<Result<IngestEventsResponse>> HandleAsync(IngestEventsCommand command, CancellationToken cancellationToken)
    {
        if (command.SiteId != command.AuthenticatedSiteId)
        {
            return DoorEventErrors.SiteMismatch;
        }

        var now = clock.GetUtcNow();
        var events = new List<DoorEvent>(command.Events.Count);
        foreach (var e in command.Events)
        {
            var created = DoorEvent.Create(command.SiteId, e.EventId, e.DoorId, Enum.Parse<DoorEventKind>(e.Type), e.OccurredAt, e.CardNumber, e.DenyReason, e.AccessListSequence, now);
            if (created.IsFailure)
            {
                return created.Error!;
            }

            events.Add(created.Value);
        }

        var inserted = await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var stored = await store.InsertNewAsync(events, ct);
                var correlationId = Correlation.Current();
                foreach (var group in stored.GroupBy(e => e.Type))
                {
                    await publisher.PublishAsync(
                        new DoorEventsRecorded(
                            Guid.CreateVersion7(), now, correlationId, command.SiteId, (DoorEventType)(int)group.Key,
                            group.OrderBy(e => e.OccurredAt)
                                .Select(e => new RecordedDoorEvent(e.EventId, e.DoorId, e.OccurredAt, e.CardNumber, e.DenyReason, e.AccessListSequence, e.RecordedAt))
                                .ToList()),
                        ct);
                }

                return stored;
            },
            cancellationToken);

        var newIds = inserted.Select(e => e.EventId).ToHashSet();
        foreach (var group in events.GroupBy(e => (e.Type, IsNew: newIds.Contains(e.EventId))))
        {
            metrics.Record(group.Key.Type, group.Key.IsNew, group.Count());
        }

        return new IngestEventsResponse(inserted.Count, events.Count - inserted.Count);
    }
}
