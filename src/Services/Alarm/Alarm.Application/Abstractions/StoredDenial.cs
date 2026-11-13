namespace AccessHub.Alarm.Application.Abstractions;

public sealed record StoredDenial(Guid SourceEventId, string CardNumber, DateTimeOffset OccurredAt);
