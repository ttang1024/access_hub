namespace AccessHub.Alarm.Application.Messages;

/// <param name="AlarmId">The alarm.</param>
/// <param name="Reason">NotFound, InvalidTransition or NoteRequired.</param>
/// <param name="CurrentState">The alarm's state when the command was rejected, if it exists.</param>
public sealed record AlarmCommandRejected(Guid AlarmId, string Reason, string? CurrentState);
