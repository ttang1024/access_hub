namespace AccessHub.Alarm.Application.Abstractions;

public sealed record AlarmDto(
    Guid Id, string Rule, string Priority, string State, bool Escalated, Guid SiteId, Guid? DoorId, string? CardNumber, int OccurrenceCount,
    DateTimeOffset RaisedAt, DateTimeOffset LastTriggeredAt, string? AcknowledgedBy, DateTimeOffset? AcknowledgedAt, string? ClosedBy, DateTimeOffset? ClosedAt, string? CloseNote);
