using System.Data;
using AccessHub.AccessControl.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

internal sealed class SiteChangeLog(AccessControlDbContext db) : ISiteChangeLog
{
    public async Task<SiteSnapshot?> GetSnapshotAsync(Guid siteId, CancellationToken cancellationToken)
    {
        // Repeatable read: the sequence and the entries come from the same snapshot of the database.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var site = await db.Sites.AsNoTracking()
            .Where(s => s.SiteId == siteId)
            .Select(s => new { s.TimeZoneId, s.LastSequence })
            .SingleOrDefaultAsync(cancellationToken);
        if (site is null)
        {
            return null;
        }

        var entries = await db.SiteEntries.AsNoTracking()
            .Where(e => e.SiteId == siteId)
            .OrderBy(e => e.Kind).ThenBy(e => e.EntityId)
            .Select(e => new SiteEntryRecord(e.Kind, e.EntityId, e.Payload))
            .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SiteSnapshot(siteId, site.TimeZoneId, site.LastSequence, entries);
    }

    public async Task<ChangeLogPosition?> GetPositionAsync(Guid siteId, CancellationToken cancellationToken) =>
        await db.Sites.AsNoTracking()
            .Where(s => s.SiteId == siteId)
            .Select(s => new ChangeLogPosition(s.LastSequence, s.PrunedThroughSequence))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SiteChangeRecord>> ReadChangesAsync(Guid siteId, long afterSequence, int limit, CancellationToken cancellationToken) =>
        await db.SiteChanges.AsNoTracking()
            .Where(c => c.SiteId == siteId && c.Sequence > afterSequence)
            .OrderBy(c => c.Sequence)
            .Take(limit)
            .Select(c => new SiteChangeRecord(c.SiteId, c.Sequence, c.Kind, c.EntityId, c.Payload, c.ChangedAt))
            .ToListAsync(cancellationToken);
}
