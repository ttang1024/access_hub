namespace AccessHub.Alarm.Application.Abstractions;

public sealed record SiteEventDto(Guid EventId, Guid SiteId, Guid DoorId, string Type, string? CardNumber, string? DenyReason, DateTimeOffset OccurredAt, DateTimeOffset RecordedAt);
