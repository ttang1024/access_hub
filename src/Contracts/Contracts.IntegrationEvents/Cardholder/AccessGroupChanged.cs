namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>An access group was created, changed or deleted. Carries the full door list with schedules.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the change happened.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="AccessGroupId">Access group ID.</param>
/// <param name="Name">Group name.</param>
/// <param name="IsDeleted">True when the group was deleted; <paramref name="Doors"/> is then empty.</param>
/// <param name="Doors">Doors the group opens, each with its weekly schedule.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record AccessGroupChanged(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid AccessGroupId, string Name, bool IsDeleted, IReadOnlyList<AccessGroupDoor> Doors, int Version) : IVersionedStateEvent;

/// <summary>A door in an access group, with the weekly windows when the group may open it.</summary>
/// <param name="DoorId">Door ID.</param>
/// <param name="SiteId">Site of the door, so consumers know which site's access list is affected.</param>
/// <param name="Schedule">Weekly windows in the site's local time.</param>
public sealed record AccessGroupDoor(Guid DoorId, Guid SiteId, IReadOnlyList<WeeklyTimeWindow> Schedule);

/// <summary>A window on one day of the week, in the site's local time. Start inclusive, end exclusive.</summary>
/// <param name="Day">Day of week.</param>
/// <param name="StartMinute">Minutes after local midnight (0-1439).</param>
/// <param name="EndMinute">Minutes after local midnight (1-1440), greater than the start.</param>
public sealed record WeeklyTimeWindow(DayOfWeek Day, int StartMinute, int EndMinute);
