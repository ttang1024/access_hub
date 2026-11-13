using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Messages;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Application.Features.Close;

public sealed class CloseAlarmHandler(IAlarmCommandSender sender, ICurrentUser user) : ICommandHandler<CloseAlarmCommand>
{
    public async Task<Result> HandleAsync(CloseAlarmCommand command, CancellationToken cancellationToken)
    {
        var (_, rejected) = await sender.CloseAsync(new CloseAlarm(command.AlarmId, user.UserId, command.Note.Trim()), cancellationToken);
        return rejected is null ? Result.Success() : AlarmCommandErrors.ToError(rejected, "closed");
    }
}
