using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.AccessGroups;

public static class AccessGroupErrors
{
    public static Error NotFound(AccessGroupId id) => Error.NotFound("AccessGroup.NotFound", $"Access group {id} was not found.");

    public static Error NotFoundMany(IEnumerable<Guid> ids) => Error.NotFound("AccessGroup.NotFound", $"Access groups not found: {string.Join(", ", ids)}.");

    public static Error InvalidName => Error.Validation("AccessGroup.InvalidName", "Access group name is required and at most 200 characters.");

    public static Error DuplicateDoor => Error.Validation("AccessGroup.DuplicateDoor", "A door can appear only once in an access group.");

    public static Error TooManyDoors => Error.Validation("AccessGroup.TooManyDoors", "An access group can include at most 500 doors.");

    public static Error Deleted(AccessGroupId id) => Error.Conflict("AccessGroup.Deleted", $"Access group {id} is deleted.");

    public static Error VersionMismatch(int expected, int actual) =>
        Error.Conflict("AccessGroup.VersionMismatch", $"Expected version {expected} but the access group is at version {actual}.");

    public static Error InvalidTimeWindow(DayOfWeek day, int start, int end) =>
        Error.Validation("AccessGroup.InvalidTimeWindow", $"Window {day} {start}-{end} is invalid: minutes must satisfy 0 <= start < end <= 1440.");

    public static Error InvalidSchedule(string reason) => Error.Validation("AccessGroup.InvalidSchedule", reason);
}
