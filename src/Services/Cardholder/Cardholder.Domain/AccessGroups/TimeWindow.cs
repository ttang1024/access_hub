using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.AccessGroups;

/// <summary>A window on one weekday in the site's local time. Start inclusive, end exclusive, minutes after midnight.</summary>
public sealed record TimeWindow : IValueObject
{
    public const int MinutesPerDay = 24 * 60;

    private TimeWindow(DayOfWeek day, int startMinute, int endMinute)
    {
        Day = day;
        StartMinute = startMinute;
        EndMinute = endMinute;
    }

    public DayOfWeek Day { get; }

    public int StartMinute { get; }

    public int EndMinute { get; }

    public static Result<TimeWindow> Create(DayOfWeek day, int startMinute, int endMinute)
    {
        if (!Enum.IsDefined(day) || startMinute < 0 || endMinute > MinutesPerDay || startMinute >= endMinute)
        {
            return AccessGroupErrors.InvalidTimeWindow(day, startMinute, endMinute);
        }

        return new TimeWindow(day, startMinute, endMinute);
    }

    public bool Overlaps(TimeWindow other) => Day == other.Day && StartMinute < other.EndMinute && other.StartMinute < EndMinute;
}
