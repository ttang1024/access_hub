namespace AccessHub.Alarm.Application.Features.Close;

public sealed record CloseAlarmCommand(Guid AlarmId, string Note);
