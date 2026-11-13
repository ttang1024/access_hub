using AccessHub.Alarm.Application.Messages;

namespace AccessHub.Alarm.Application.Abstractions;

/// <summary>Sends a lifecycle command to the alarm saga and waits for its verdict.</summary>
public interface IAlarmCommandSender
{
    Task<(AlarmCommandAccepted? Accepted, AlarmCommandRejected? Rejected)> AcknowledgeAsync(AcknowledgeAlarm command, CancellationToken cancellationToken);

    Task<(AlarmCommandAccepted? Accepted, AlarmCommandRejected? Rejected)> CloseAsync(CloseAlarm command, CancellationToken cancellationToken);
}
