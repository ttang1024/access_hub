using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>One row of a site's change log. Sequence is per site, starts at 1 and has no gaps.</summary>
public sealed class SiteChange
{
    private SiteChange()
    {
    }

    public SiteChange(Guid siteId, long sequence, AccessListEntryKind kind, Guid entityId, string? payload, DateTimeOffset changedAt, DateTimeOffset recordedAt)
    {
        SiteId = siteId;
        Sequence = sequence;
        Kind = kind;
        EntityId = entityId;
        Payload = payload;
        ChangedAt = changedAt;
        RecordedAt = recordedAt;
    }

    public Guid SiteId { get; private set; }

    public long Sequence { get; private set; }

    public AccessListEntryKind Kind { get; private set; }

    public Guid EntityId { get; private set; }

    /// <summary>Null is a tombstone: the entity left the site's list.</summary>
    public string? Payload { get; private set; }

    /// <summary>The source Cardholder event's OccurredAt.</summary>
    public DateTimeOffset ChangedAt { get; private set; }

    /// <summary>When the change was appended; retention is measured from here.</summary>
    public DateTimeOffset RecordedAt { get; private set; }
}
