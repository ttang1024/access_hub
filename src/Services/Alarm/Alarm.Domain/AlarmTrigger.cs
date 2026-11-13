namespace AccessHub.Alarm.Domain;

/// <summary>A rule fired. At most one open alarm exists per <see cref="SubjectKey"/>; further triggers add occurrences.</summary>
public sealed record AlarmTrigger(
    AlarmRule Rule, AlarmPriority Priority, string SubjectKey, Guid SiteId, Guid? DoorId, string? CardNumber, Guid SourceEventId, DateTimeOffset TriggeredAt)
{
    public static AlarmTrigger ForDoor(AlarmRule rule, AlarmPriority priority, Guid siteId, Guid doorId, Guid sourceEventId, DateTimeOffset at) =>
        new(rule, priority, $"{rule}:door:{doorId}", siteId, doorId, null, sourceEventId, at);

    public static AlarmTrigger ForCard(AlarmRule rule, AlarmPriority priority, Guid siteId, Guid? doorId, string cardNumber, Guid sourceEventId, DateTimeOffset at) =>
        new(rule, priority, $"{rule}:card:{cardNumber}", siteId, doorId, cardNumber, sourceEventId, at);
}
