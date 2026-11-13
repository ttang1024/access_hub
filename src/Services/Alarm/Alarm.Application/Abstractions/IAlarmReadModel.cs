using AccessHub.BuildingBlocks.Application;

namespace AccessHub.Alarm.Application.Abstractions;

public interface IAlarmReadModel
{
    Task<AlarmDto?> GetAsync(Guid alarmId, CancellationToken cancellationToken);

    /// <summary>Lists alarms, newest first.</summary>
    /// <param name="state">Raised, Acknowledged, Closed, "open" (not closed), or null for all.</param>
    /// <param name="page">Paging.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<PagedResult<AlarmDto>> ListAsync(string? state, PageRequest page, CancellationToken cancellationToken);
}
