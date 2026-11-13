using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.EventIngestion.Domain.Events;

/// <summary>A site event, stored append-only. Identified by the site-generated event ID, unique per site.</summary>
public sealed class DoorEvent
{
    /// <summary>Clock skew tolerated between site and cloud.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private DoorEvent()
    {
    }

    public Guid SiteId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid DoorId { get; private set; }

    public DoorEventKind Type { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? CardNumber { get; private set; }

    public string? DenyReason { get; private set; }

    public long? AccessListSequence { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public bool IsDecision => Type is DoorEventKind.AccessGranted or DoorEventKind.AccessDenied;

    public static Result<DoorEvent> Create(
        Guid siteId, Guid eventId, Guid doorId, DoorEventKind type, DateTimeOffset occurredAt, string? cardNumber, string? denyReason, long? accessListSequence, DateTimeOffset now)
    {
        var e = new DoorEvent
        {
            SiteId = siteId,
            EventId = eventId,
            DoorId = doorId,
            Type = type,
            OccurredAt = occurredAt.ToUniversalTime(),
            CardNumber = string.IsNullOrWhiteSpace(cardNumber) ? null : cardNumber.Trim().ToUpperInvariant(),
            DenyReason = type == DoorEventKind.AccessDenied ? denyReason : null,
            AccessListSequence = accessListSequence,
            RecordedAt = now,
        };

        if (e.IsDecision && e.CardNumber is null)
        {
            return DoorEventErrors.MissingCard(eventId);
        }

        if (type == DoorEventKind.AccessDenied && (denyReason is null || !DoorEventErrors.DenyReasons.Contains(denyReason)))
        {
            return DoorEventErrors.InvalidDenyReason(eventId);
        }

        // Out-of-order and old events are fine (sites buffer through outages); only the future is suspicious.
        return occurredAt > now + MaxClockSkew ? DoorEventErrors.InFuture(eventId) : e;
    }
}
