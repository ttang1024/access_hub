using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>
/// Recomputes what each affected site should hold for each target entity and appends a change only where the result
/// differs from what the site holds. This makes projections idempotent by construction: a redelivered or stale event
/// produces the same desired entries and therefore no changes.
/// </summary>
public sealed class SiteListReconciler(IProjectionStore store)
{
    public async Task ReconcileAsync(IReadOnlyCollection<ReconcileTarget> targets, DateTimeOffset changedAt, CancellationToken cancellationToken)
    {
        await store.SaveChangesAsync(cancellationToken);

        // Deterministic order (site, kind, entity) so sequences are reproducible and lock order is stable.
        var work = new SortedSet<(Guid Site, AccessListEntryKind Kind, Guid Entity)>();
        foreach (var target in targets)
        {
            var holding = await store.GetSitesHoldingEntryAsync(target.Kind, target.EntityId, cancellationToken);
            foreach (var site in target.CandidateSites.Concat(holding))
            {
                work.Add((site, target.Kind, target.EntityId));
            }
        }

        foreach (var (site, kind, entity) in work)
        {
            var desired = EntryPayloads.Serialize(await DesiredEntryAsync(site, kind, entity, cancellationToken));
            var current = await store.GetEntryPayloadAsync(site, kind, entity, cancellationToken);
            if (!string.Equals(desired, current, StringComparison.Ordinal))
            {
                await store.AppendChangeAsync(site, kind, entity, desired, changedAt, cancellationToken);
            }
        }
    }

    private async Task<object?> DesiredEntryAsync(Guid siteId, AccessListEntryKind kind, Guid entityId, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case AccessListEntryKind.Door:
                return SiteEntryCalculator.ForDoor(await store.FindDoorAsync(entityId, cancellationToken), siteId);
            case AccessListEntryKind.AccessGroup:
                return SiteEntryCalculator.ForGroup(await store.FindGroupAsync(entityId, cancellationToken), siteId);
            case AccessListEntryKind.Card:
                var card = await store.FindCardAsync(entityId, cancellationToken);
                var holder = card is null ? null : await store.FindCardholderAsync(card.CardholderId, cancellationToken);
                var groups = holder is null ? new Dictionary<Guid, AccessGroupState>() : await store.GetGroupsAsync(holder.AccessGroupIds.ToList(), cancellationToken);
                return SiteEntryCalculator.ForCard(card, holder, groups, siteId);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind.");
        }
    }
}
