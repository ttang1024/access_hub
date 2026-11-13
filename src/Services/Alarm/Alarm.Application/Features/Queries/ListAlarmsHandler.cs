using AccessHub.Alarm.Application.Abstractions;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Application.Features.Queries;

public sealed class ListAlarmsHandler(IAlarmReadModel readModel) : IQueryHandler<ListAlarmsQuery, PagedResult<AlarmDto>>
{
    public async Task<Result<PagedResult<AlarmDto>>> HandleAsync(ListAlarmsQuery query, CancellationToken cancellationToken) =>
        await readModel.ListAsync(query.State, new PageRequest(query.Page, query.PageSize), cancellationToken);
}
