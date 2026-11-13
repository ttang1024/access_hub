using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Domain;
using AccessHub.Alarm.Infrastructure.Sagas;
using AccessHub.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Alarm.Infrastructure.Persistence;

internal sealed class AlarmReadModel(AlarmDbContext db) : IAlarmReadModel
{
    public Task<AlarmDto?> GetAsync(Guid alarmId, CancellationToken cancellationToken) =>
        db.Alarms.AsNoTracking().Where(a => a.CorrelationId == alarmId).Select(ToDto).SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<AlarmDto>> ListAsync(string? state, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Alarms.AsNoTracking();
        if (string.Equals(state, "open", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.CurrentState != AlarmStatus.Closed);
        }
        else if (state is not null)
        {
            var normalised = new[] { AlarmStatus.Raised, AlarmStatus.Acknowledged, AlarmStatus.Closed }.Single(s => s.Equals(state, StringComparison.OrdinalIgnoreCase));
            query = query.Where(a => a.CurrentState == normalised);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.RaisedAt).ThenBy(a => a.CorrelationId).Skip(page.Skip).Take(page.Take).Select(ToDto).ToListAsync(cancellationToken);
        return new PagedResult<AlarmDto>(items, page.Page, page.Take, total);
    }

    private static readonly System.Linq.Expressions.Expression<Func<AlarmState, AlarmDto>> ToDto = a => new AlarmDto(
        a.CorrelationId, a.Rule, a.Priority, a.CurrentState, a.Escalated, a.SiteId, a.DoorId, a.CardNumber, a.OccurrenceCount,
        a.RaisedAt, a.LastTriggeredAt, a.AcknowledgedBy, a.AcknowledgedAt, a.ClosedBy, a.ClosedAt, a.CloseNote);
}
