using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Domain;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.AccessControl;

namespace AccessHub.Alarm.Application.Features.Rules;

public sealed class EvaluateStaleGrantHandler(IAlarmTriggerSink triggers) : ICommandHandler<StaleAccessGranted>
{
    public async Task<Result> HandleAsync(StaleAccessGranted e, CancellationToken cancellationToken)
    {
        await triggers.TriggerAsync(AlarmRules.StaleGrant(e.SiteId, e.DoorId, e.CardNumber, e.SourceEventId, e.GrantedAt), cancellationToken);
        return Result.Success();
    }
}
