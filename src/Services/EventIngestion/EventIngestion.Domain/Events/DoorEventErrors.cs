using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.EventIngestion.Domain.Events;

public static class DoorEventErrors
{
    public static readonly IReadOnlySet<string> DenyReasons = new HashSet<string>(StringComparer.Ordinal)
    {
        "UnknownCard", "CardRevoked", "CardExpired", "CardholderInactive", "NoAccessToDoor", "OutsideSchedule",
    };

    public static Error MissingCard(Guid eventId) => Error.Validation("DoorEvent.MissingCard", $"Event {eventId}: decision events need a card number.");

    public static Error InvalidDenyReason(Guid eventId) => Error.Validation("DoorEvent.InvalidDenyReason", $"Event {eventId}: AccessDenied needs a valid deny reason.");

    public static Error InFuture(Guid eventId) => Error.Validation("DoorEvent.InFuture", $"Event {eventId}: OccurredAt is in the future.");

    public static Error SiteMismatch => Error.Forbidden("DoorEvent.SiteMismatch", "A site may only report its own events.");
}
