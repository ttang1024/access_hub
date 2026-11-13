namespace AccessHub.Alarm.Application.Messages;

public sealed record CloseAlarm(Guid AlarmId, string UserId, string Note);
