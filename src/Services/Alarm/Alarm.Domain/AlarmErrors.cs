using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Alarm.Domain;

public static class AlarmErrors
{
    public const int MaxNoteLength = 2000;

    public static Error NotFound(Guid id) => Error.NotFound("Alarm.NotFound", $"Alarm {id} was not found.");

    public static Error InvalidTransition(Guid id, string from, string action) =>
        Error.Conflict("Alarm.InvalidTransition", $"Alarm {id} is {from} and cannot be {action}.");

    public static Error NoteRequired => Error.Validation("Alarm.NoteRequired", $"Closing an alarm requires a note of at most {MaxNoteLength} characters.");

    public static bool IsValidNote(string? note) => !string.IsNullOrWhiteSpace(note) && note.Length <= MaxNoteLength;
}
