namespace AccessHub.AccessControl.Application.Abstractions;

/// <summary>A consistent copy of a site's list and the sequence it reflects.</summary>
public sealed record SiteSnapshot(Guid SiteId, string TimeZoneId, long Sequence, IReadOnlyList<SiteEntryRecord> Entries);
