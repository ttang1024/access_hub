using AccessHub.Alarm.Application.Messages;
using AccessHub.Alarm.Domain;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Application.Features;

internal static class AlarmCommandErrors
{
    public static Error ToError(AlarmCommandRejected rejected, string action) => rejected.Reason switch
    {
        "NotFound" => AlarmErrors.NotFound(rejected.AlarmId),
        "NoteRequired" => AlarmErrors.NoteRequired,
        _ => AlarmErrors.InvalidTransition(rejected.AlarmId, rejected.CurrentState ?? "unknown", action),
    };
}
