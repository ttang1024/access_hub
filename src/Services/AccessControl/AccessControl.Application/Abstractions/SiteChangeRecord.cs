using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Application.Abstractions;

/// <summary>One row of a site's change log. A null payload is a tombstone.</summary>
public sealed record SiteChangeRecord(Guid SiteId, long Sequence, AccessListEntryKind Kind, Guid EntityId, string? Payload, DateTimeOffset ChangedAt);
