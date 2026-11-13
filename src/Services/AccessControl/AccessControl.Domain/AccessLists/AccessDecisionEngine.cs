namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>
/// The access rules, shared by the site controller (edge) and the cloud's "explain access" endpoint so they can never
/// disagree. A pure function: no I/O, no clock, no logging.
/// </summary>
public static class AccessDecisionEngine
{
    public static AccessDecision Decide(SiteAccessList accessList, Guid doorId, string cardNumber, DateTimeOffset timestamp)
    {
        var card = accessList.FindCard(cardNumber);
        if (card is null)
        {
            return AccessDecision.Deny(DenyReason.UnknownCard);
        }

        if (card.IsRevoked)
        {
            return AccessDecision.Deny(DenyReason.CardRevoked);
        }

        if (card.ExpiresAt is { } expiresAt && timestamp >= expiresAt)
        {
            return AccessDecision.Deny(DenyReason.CardExpired);
        }

        if (!card.CardholderActive)
        {
            return AccessDecision.Deny(DenyReason.CardholderInactive);
        }

        var local = TimeZoneInfo.ConvertTime(timestamp, accessList.TimeZone);
        var minuteOfDay = local.TimeOfDay.TotalMinutes;
        var hasDoor = false;
        foreach (var groupId in card.AccessGroupIds)
        {
            var door = accessList.FindGroup(groupId)?.FindDoor(doorId);
            if (door is null)
            {
                continue;
            }

            hasDoor = true;
            foreach (var window in door.Windows)
            {
                if (window.Contains(local.DayOfWeek, minuteOfDay))
                {
                    return AccessDecision.Grant();
                }
            }
        }

        return AccessDecision.Deny(hasDoor ? DenyReason.OutsideSchedule : DenyReason.NoAccessToDoor);
    }
}
