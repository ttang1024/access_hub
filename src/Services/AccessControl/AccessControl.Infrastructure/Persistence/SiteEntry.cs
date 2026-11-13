using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>What a site's list currently holds for one entity. The materialised list that snapshots are read from.</summary>
public sealed class SiteEntry
{
    private SiteEntry()
    {
    }

    public SiteEntry(Guid siteId, AccessListEntryKind kind, Guid entityId, string payload)
    {
        SiteId = siteId;
        Kind = kind;
        EntityId = entityId;
        Payload = payload;
    }

    public Guid SiteId { get; private set; }

    public AccessListEntryKind Kind { get; private set; }

    public Guid EntityId { get; private set; }

    /// <summary>Stored as text, not jsonb, so it compares byte for byte with a freshly serialised payload.</summary>
    public string Payload { get; set; } = string.Empty;
}
