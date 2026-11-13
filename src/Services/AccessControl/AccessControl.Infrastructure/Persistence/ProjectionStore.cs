using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

internal sealed class ProjectionStore(AccessControlDbContext db, TimeProvider clock) : IProjectionStore
{
    // Arbitrary constant key for the transaction-scoped advisory lock that serialises projections.
    private const long ProjectionLockKey = 0x4143_4345_5353; // "ACCESS"

    public async Task AcquireProjectionLockAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Projections must run inside a transaction (the consumer outbox provides one).");
        }

        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({ProjectionLockKey})", cancellationToken);
    }

    public async Task<SiteState?> FindSiteAsync(Guid siteId, CancellationToken cancellationToken) =>
        await db.Sites.FindAsync([siteId], cancellationToken);

    public async Task<DoorState?> FindDoorAsync(Guid doorId, CancellationToken cancellationToken) =>
        await db.Doors.FindAsync([doorId], cancellationToken);

    public async Task<AccessGroupState?> FindGroupAsync(Guid accessGroupId, CancellationToken cancellationToken) =>
        await db.AccessGroups.FindAsync([accessGroupId], cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, AccessGroupState>> GetGroupsAsync(IReadOnlyCollection<Guid> accessGroupIds, CancellationToken cancellationToken)
    {
        if (accessGroupIds.Count == 0)
        {
            return new Dictionary<Guid, AccessGroupState>();
        }

        var ids = accessGroupIds.ToList();
        return await db.AccessGroups.Where(g => ids.Contains(g.AccessGroupId)).ToDictionaryAsync(g => g.AccessGroupId, cancellationToken);
    }

    public async Task<CardholderState?> FindCardholderAsync(Guid cardholderId, CancellationToken cancellationToken) =>
        await db.Cardholders.FindAsync([cardholderId], cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetCardholderIdsInGroupAsync(Guid accessGroupId, CancellationToken cancellationToken) =>
        await db.Database
            .SqlQuery<Guid>($"SELECT cardholder_id AS \"Value\" FROM access_control.cardholders WHERE access_group_ids @> ARRAY[{accessGroupId}]::uuid[]")
            .ToListAsync(cancellationToken);

    public async Task<CardState?> FindCardAsync(Guid cardId, CancellationToken cancellationToken) =>
        await db.Cards.FindAsync([cardId], cancellationToken);

    public async Task<IReadOnlyList<CardState>> GetCardsOfCardholdersAsync(IReadOnlyCollection<Guid> cardholderIds, CancellationToken cancellationToken)
    {
        if (cardholderIds.Count == 0)
        {
            return [];
        }

        var ids = cardholderIds.ToList();
        return await db.Cards.Where(c => ids.Contains(c.CardholderId)).ToListAsync(cancellationToken);
    }

    public void Add(SiteState site) => db.Sites.Add(site);

    public void Add(DoorState door) => db.Doors.Add(door);

    public void Add(AccessGroupState group) => db.AccessGroups.Add(group);

    public void Add(CardholderState cardholder) => db.Cardholders.Add(cardholder);

    public void Add(CardState card) => db.Cards.Add(card);

    public async Task<IReadOnlyList<Guid>> GetSitesHoldingEntryAsync(AccessListEntryKind kind, Guid entityId, CancellationToken cancellationToken) =>
        await db.SiteEntries.AsNoTracking().Where(e => e.Kind == kind && e.EntityId == entityId).Select(e => e.SiteId).ToListAsync(cancellationToken);

    public async Task<string?> GetEntryPayloadAsync(Guid siteId, AccessListEntryKind kind, Guid entityId, CancellationToken cancellationToken) =>
        (await db.SiteEntries.FindAsync([siteId, kind, entityId], cancellationToken))?.Payload;

    public async Task AppendChangeAsync(Guid siteId, AccessListEntryKind kind, Guid entityId, string? payload, DateTimeOffset changedAt, CancellationToken cancellationToken)
    {
        // A change can arrive before SiteRegistered; the site row is the sequence counter, so make sure it exists.
        await db.SaveChangesAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO access_control.sites (site_id, name, time_zone_id, version, last_sequence, pruned_through_sequence) VALUES ({siteId}, '', 'UTC', 0, 0, 0) ON CONFLICT (site_id) DO NOTHING",
            cancellationToken);

        // The row lock is held until commit, so sequences for a site commit in the order they were allocated
        // and a reader can never see sequence N+1 before N.
        var site = (await db.Sites.FromSql($"SELECT * FROM access_control.sites WHERE site_id = {siteId} FOR UPDATE").ToListAsync(cancellationToken)).Single();
        var sequence = site.AllocateSequence();

        var entry = await db.SiteEntries.FindAsync([siteId, kind, entityId], cancellationToken);
        if (payload is null)
        {
            if (entry is not null)
            {
                db.SiteEntries.Remove(entry);
            }
        }
        else if (entry is null)
        {
            db.SiteEntries.Add(new SiteEntry(siteId, kind, entityId, payload));
        }
        else
        {
            entry.Payload = payload;
        }

        db.SiteChanges.Add(new SiteChange(siteId, sequence, kind, entityId, payload, changedAt, clock.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
