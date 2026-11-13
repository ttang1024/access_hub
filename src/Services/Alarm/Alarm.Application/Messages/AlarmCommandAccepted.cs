namespace AccessHub.Alarm.Application.Messages;

public sealed record AlarmCommandAccepted(Guid AlarmId, string State);
