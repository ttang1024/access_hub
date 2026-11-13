using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Domain;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Application.Features.Queries;

public sealed class GetAlarmHandler(IAlarmReadModel readModel) : IQueryHandler<GetAlarmQuery, AlarmDto>
{
    public async Task<Result<AlarmDto>> HandleAsync(GetAlarmQuery query, CancellationToken cancellationToken) =>
        await readModel.GetAsync(query.AlarmId, cancellationToken) is { } alarm ? alarm : AlarmErrors.NotFound(query.AlarmId);
}
