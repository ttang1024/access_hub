using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.AccessGroups;

/// <summary>A weekly schedule: one or more non-overlapping windows.</summary>
public sealed record Schedule : IValueObject
{
    public const int MaxWindows = 28;

    private Schedule(IReadOnlyList<TimeWindow> windows) => Windows = windows;

    public IReadOnlyList<TimeWindow> Windows { get; }

    public static Result<Schedule> Create(IReadOnlyList<TimeWindow> windows)
    {
        if (windows.Count is 0 or > MaxWindows)
        {
            return AccessGroupErrors.InvalidSchedule($"A schedule needs between 1 and {MaxWindows} windows.");
        }

        for (var i = 0; i < windows.Count; i++)
        {
            for (var j = i + 1; j < windows.Count; j++)
            {
                if (windows[i].Overlaps(windows[j]))
                {
                    return AccessGroupErrors.InvalidSchedule($"Windows on {windows[i].Day} overlap.");
                }
            }
        }

        return new Schedule(windows.OrderBy(w => w.Day).ThenBy(w => w.StartMinute).ToList());
    }

    public bool Equals(Schedule? other) => other is not null && Windows.SequenceEqual(other.Windows);

    public override int GetHashCode() => Windows.Aggregate(0, (h, w) => HashCode.Combine(h, w));
}
