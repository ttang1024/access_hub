namespace AccessHub.Alarm.Application.Messages;

public sealed record AcknowledgeAlarm(Guid AlarmId, string UserId);
