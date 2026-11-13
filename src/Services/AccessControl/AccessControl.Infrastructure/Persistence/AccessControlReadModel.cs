using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Projection;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.AccessControl.Domain.AccessLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

internal sealed class AccessControlReadModel(AccessControlDbContext db, ISiteChangeLog changeLog, IOptions<SyncOptions> options, TimeProvider clock) : IAccessControlReadModel
{
    public async Task<IReadOnlyList<SiteSyncStatusDto>> GetSyncStatusAsync(CancellationToken cancellationToken)
    {
        // A stream counts as connected only while it keeps sending heartbeats, so a replica that died without
        // closing its streams does not leave sites looking connected forever.
        var liveSince = clock.GetUtcNow() - (options.Value.HeartbeatInterval * 3);
        var rows = await (
                from site in db.Sites.AsNoTracking()
                where site.Version > 0
                join status in db.SyncStatus.AsNoTracking() on site.SiteId equals status.SiteId into statuses
                from status in statuses.DefaultIfEmpty()
                orderby site.Name
                select new { site.SiteId, site.Name, site.LastSequence, Status = status })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            var delivered = r.Status?.LastDeliveredSequence ?? 0;
            var connected = r.Status is { IsConnected: true, LastSeenAt: { } seen } && seen >= liveSince;
            return new SiteSyncStatusDto(r.SiteId, r.Name, connected, r.Status?.LastConnectedAt, r.Status?.LastSeenAt, r.LastSequence, delivered, Math.Max(r.LastSequence - delivered, 0));
        }).ToList();
    }

    public async Task<(SiteAccessList List, long Sequence)?> LoadAccessListAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var snapshot = await changeLog.GetSnapshotAsync(siteId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var list = new SiteAccessList(snapshot.TimeZoneId);
        foreach (var entry in snapshot.Entries)
        {
            EntryPayloads.ApplyTo(list, entry.Kind, entry.EntityId, entry.Payload);
        }

        return (list, snapshot.Sequence);
    }
}
