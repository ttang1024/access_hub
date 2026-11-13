using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Doors;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

/// <summary>Turns door inputs into domain <see cref="DoorAccess"/> values, resolving each door's site.</summary>
internal static class DoorAccessBuilder
{
    public static async Task<Result<IReadOnlyList<DoorAccess>>> BuildAsync(
        IDoorRepository doorRepository, IReadOnlyList<AccessGroupDoorInput> inputs, CancellationToken cancellationToken)
    {
        var ids = inputs.Select(i => new DoorId(i.DoorId)).Distinct().ToList();
        var doors = (await doorRepository.GetManyAsync(ids, cancellationToken)).ToDictionary(d => d.Id);
        var result = new List<DoorAccess>(inputs.Count);
        foreach (var input in inputs)
        {
            var doorId = new DoorId(input.DoorId);
            if (!doors.TryGetValue(doorId, out var door))
            {
                return DoorErrors.NotFound(doorId);
            }

            var windows = new List<TimeWindow>(input.Windows.Count);
            foreach (var w in input.Windows)
            {
                var window = TimeWindow.Create(w.Day, w.StartMinute, w.EndMinute);
                if (window.IsFailure)
                {
                    return window.Error!;
                }

                windows.Add(window.Value);
            }

            var schedule = Schedule.Create(windows);
            if (schedule.IsFailure)
            {
                return schedule.Error!;
            }

            result.Add(new DoorAccess(doorId, door.SiteId, schedule.Value));
        }

        return result;
    }

    public static object Summary(AccessGroup group) => new
    {
        group.Name,
        group.IsDeleted,
        Doors = group.Doors.Select(d => new { DoorId = d.DoorId.Value, Windows = d.Schedule.Windows.Count }).ToList(),
        group.Version,
    };
}
