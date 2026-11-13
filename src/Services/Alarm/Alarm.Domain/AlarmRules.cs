namespace AccessHub.Alarm.Domain;

/// <summary>The alarm rules. Pure: the caller supplies recent denials for the repeated-denials rule.</summary>
public static class AlarmRules
{
    public const int DenialThreshold = 3;
    public static readonly TimeSpan DenialWindow = TimeSpan.FromSeconds(60);

    public static AlarmTrigger? ForDoorEvent(string eventType, Guid siteId, Guid doorId, Guid sourceEventId, DateTimeOffset occurredAt) => eventType switch
    {
        "DoorForced" => AlarmTrigger.ForDoor(AlarmRule.DoorForced, AlarmPriority.High, siteId, doorId, sourceEventId, occurredAt),
        "DoorHeldOpen" => AlarmTrigger.ForDoor(AlarmRule.DoorHeldOpen, AlarmPriority.Medium, siteId, doorId, sourceEventId, occurredAt),
        _ => null,
    };

    public static AlarmTrigger StaleGrant(Guid siteId, Guid doorId, string cardNumber, Guid sourceEventId, DateTimeOffset grantedAt) =>
        AlarmTrigger.ForCard(AlarmRule.StaleGrant, AlarmPriority.High, siteId, doorId, cardNumber, sourceEventId, grantedAt);

    /// <summary>
    /// True when <paramref name="newDenial"/> lies in some 60-second window holding at least three denials for the card.
    /// Works for any arrival order: <paramref name="denials"/> is every stored denial near the new one, by OccurredAt.
    /// </summary>
    public static bool RepeatedDenialsTriggered(IReadOnlyCollection<DateTimeOffset> denials, DateTimeOffset newDenial)
    {
        var times = denials.Where(t => (t - newDenial).Duration() <= DenialWindow).Order().ToList();
        for (var start = 0; start < times.Count; start++)
        {
            var end = start;
            while (end + 1 < times.Count && times[end + 1] - times[start] <= DenialWindow)
            {
                end++;
            }

            var containsNew = times[start] <= newDenial && newDenial <= times[end];
            if (containsNew && end - start + 1 >= DenialThreshold)
            {
                return true;
            }
        }

        return false;
    }
}
