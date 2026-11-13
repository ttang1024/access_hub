namespace AccessHub.Alarm.Domain;

/// <summary>Lifecycle states. Names match the saga's state names.</summary>
public static class AlarmStatus
{
    public const string Raised = nameof(Raised);
    public const string Acknowledged = nameof(Acknowledged);
    public const string Closed = nameof(Closed);
}
