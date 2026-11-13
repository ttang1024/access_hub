namespace AccessHub.Contracts.IntegrationEvents.EventIngestion;

/// <summary>
/// Site events stored for the first time, grouped per stored batch and event type: one message per type per upload,
/// instead of one per event (ADR 0011). Every stored event appears in exactly one message. Consumers usually handle
/// the per-event view from <see cref="Expand"/>.
/// </summary>
/// <param name="EventId">ID of this integration event.</param>
/// <param name="OccurredAt">When the cloud stored the batch.</param>
/// <param name="CorrelationId">Correlation ID (the upload's trace).</param>
/// <param name="SiteId">Reporting site.</param>
/// <param name="Type">The type of every event in <paramref name="Events"/>; also the routing key.</param>
/// <param name="Events">The newly stored events, in OccurredAt order.</param>
public sealed record DoorEventsRecorded(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId, Guid SiteId, DoorEventType Type, IReadOnlyList<RecordedDoorEvent> Events) : IIntegrationEvent
{
    /// <summary>
    /// The per-event view. Each item's <see cref="DoorEventRecorded.EventId"/> is the site-generated event ID, which
    /// is unique per site and stable across redeliveries.
    /// </summary>
    public IEnumerable<DoorEventRecorded> Expand() =>
        Events.Select(e => new DoorEventRecorded(
            e.SourceEventId, e.OccurredAt, CorrelationId, SiteId, e.SourceEventId, e.DoorId, Type, e.CardNumber, e.DenyReason, e.AccessListSequence, e.RecordedAt));
}
