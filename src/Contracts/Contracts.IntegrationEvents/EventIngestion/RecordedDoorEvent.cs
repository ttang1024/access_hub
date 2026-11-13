namespace AccessHub.Contracts.IntegrationEvents.EventIngestion;

/// <summary>One stored site event inside a <see cref="DoorEventsRecorded"/> message.</summary>
/// <param name="SourceEventId">The site-generated event ID (unique per site).</param>
/// <param name="DoorId">The door.</param>
/// <param name="OccurredAt">When the event happened at the site.</param>
/// <param name="CardNumber">Card presented, for decision events.</param>
/// <param name="DenyReason">Deny reason, for AccessDenied.</param>
/// <param name="AccessListSequence">Access list sequence the site had applied when it decided.</param>
/// <param name="RecordedAt">When the cloud stored the event.</param>
public sealed record RecordedDoorEvent(
    Guid SourceEventId, Guid DoorId, DateTimeOffset OccurredAt, string? CardNumber, string? DenyReason, long? AccessListSequence, DateTimeOffset RecordedAt);
