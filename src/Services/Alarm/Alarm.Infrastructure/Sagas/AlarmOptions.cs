namespace AccessHub.Alarm.Infrastructure.Sagas;

public sealed class AlarmOptions
{
    public const string Section = "Alarms";

    /// <summary>A high-priority alarm still Raised after this long escalates.</summary>
    public TimeSpan EscalationDelay { get; set; } = TimeSpan.FromMinutes(2);
}
