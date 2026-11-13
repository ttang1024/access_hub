namespace AccessHub.Alarm.Application.Messages;

/// <summary>Internal: an open alarm was triggered again (occurrence count increased). Drives dashboard updates.</summary>
public sealed record AlarmOccurrenceRecorded(Guid AlarmId, int OccurrenceCount, DateTimeOffset LastTriggeredAt);
