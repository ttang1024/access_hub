using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Messages;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Application.Features.Acknowledge;

public sealed class AcknowledgeAlarmHandler(IAlarmCommandSender sender, ICurrentUser user) : ICommandHandler<AcknowledgeAlarmCommand>
{
    public async Task<Result> HandleAsync(AcknowledgeAlarmCommand command, CancellationToken cancellationToken)
    {
        var (_, rejected) = await sender.AcknowledgeAsync(new AcknowledgeAlarm(command.AlarmId, user.UserId), cancellationToken);
        return rejected is null ? Result.Success() : AlarmCommandErrors.ToError(rejected, "acknowledged");
    }
}
