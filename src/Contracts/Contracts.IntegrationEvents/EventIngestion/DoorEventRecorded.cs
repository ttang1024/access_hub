namespace AccessHub.Contracts.IntegrationEvents.EventIngestion;

/// <summary>
/// One stored site event: the per-event view consumers handle. On the bus, stored events travel batched in
/// <see cref="DoorEventsRecorded"/> (ADR 0011); <see cref="DoorEventsRecorded.Expand"/> produces this view.
/// </summary>
/// <param name="EventId">ID of this integration event.</param>
/// <param name="OccurredAt">When the door event happened at the site (not when it arrived).</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="SiteId">Reporting site.</param>
/// <param name="SourceEventId">The site-generated event ID.</param>
/// <param name="DoorId">The door.</param>
/// <param name="Type">Event type.</param>
/// <param name="CardNumber">Card presented, for decision events.</param>
/// <param name="DenyReason">Deny reason, for AccessDenied.</param>
/// <param name="AccessListSequence">Access list sequence the site had applied when it decided.</param>
/// <param name="RecordedAt">When the cloud stored the event.</param>
public sealed record DoorEventRecorded(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid SiteId, Guid SourceEventId, Guid DoorId, DoorEventType Type,
    string? CardNumber, string? DenyReason, long? AccessListSequence, DateTimeOffset RecordedAt) : IIntegrationEvent;
