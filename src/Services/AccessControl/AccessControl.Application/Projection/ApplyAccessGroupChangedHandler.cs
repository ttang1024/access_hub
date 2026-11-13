using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>
/// A group change can move the group in or out of site lists and, with it, the cards of every member,
/// so the group and all members' cards are reconciled at every site the group touches now or touched before.
/// </summary>
public sealed class ApplyAccessGroupChangedHandler(IProjectionStore store, SiteListReconciler reconciler) : ICommandHandler<AccessGroupChanged>
{
    public async Task<Result> HandleAsync(AccessGroupChanged e, CancellationToken cancellationToken)
    {
        await store.AcquireProjectionLockAsync(cancellationToken);
        var group = await store.FindGroupAsync(e.AccessGroupId, cancellationToken);
        if (group is null)
        {
            group = AccessGroupState.Create(e.AccessGroupId);
            store.Add(group);
        }

        var sites = group.SiteIds.ToHashSet();
        var doors = e.Doors
            .Select(d => new GroupDoorState(d.DoorId, d.SiteId, d.Schedule.Select(w => new ScheduleWindow(w.Day, w.StartMinute, w.EndMinute)).ToList()))
            .ToList();
        if (!group.Apply(e.Name, e.IsDeleted, doors, e.Version))
        {
            return Result.Success();
        }

        sites.UnionWith(group.SiteIds);
        var members = await store.GetCardholderIdsInGroupAsync(e.AccessGroupId, cancellationToken);
        var cards = await store.GetCardsOfCardholdersAsync(members, cancellationToken);

        var targets = new List<ReconcileTarget> { new(AccessListEntryKind.AccessGroup, e.AccessGroupId, sites) };
        targets.AddRange(cards.Select(c => new ReconcileTarget(AccessListEntryKind.Card, c.CardId, sites)));
        await reconciler.ReconcileAsync(targets, e.OccurredAt, cancellationToken);
        return Result.Success();
    }
}
