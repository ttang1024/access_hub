using AccessHub.Alarm.Domain;

namespace AccessHub.Alarm.Application.Abstractions;

/// <summary>Hands a trigger to the alarm lifecycle (via the outbox, in the current transaction).</summary>
public interface IAlarmTriggerSink
{
    Task TriggerAsync(AlarmTrigger trigger, CancellationToken cancellationToken);
}
