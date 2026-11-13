namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>A weekly window in the site's local time: start inclusive, end exclusive, minutes after midnight.</summary>
public sealed record ScheduleWindow(DayOfWeek Day, int StartMinute, int EndMinute)
{
    public bool Contains(DayOfWeek day, double minuteOfDay) => day == Day && minuteOfDay >= StartMinute && minuteOfDay < EndMinute;
}
