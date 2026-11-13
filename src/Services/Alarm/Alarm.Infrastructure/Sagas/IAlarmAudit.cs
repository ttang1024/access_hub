namespace AccessHub.Alarm.Infrastructure.Sagas;

/// <summary>Writes the audit entry for a lifecycle command inside the saga's transaction.</summary>
public interface IAlarmAudit
{
    void Record(string action, Guid alarmId, string userId, string fromState, string toState, string? note);
}
