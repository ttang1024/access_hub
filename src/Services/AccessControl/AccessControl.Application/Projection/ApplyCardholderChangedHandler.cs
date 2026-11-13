using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>The cardholder's state is denormalised onto each card, so every card is reconciled at old and new sites.</summary>
public sealed class ApplyCardholderChangedHandler(IProjectionStore store, SiteListReconciler reconciler) : ICommandHandler<CardholderChanged>
{
    public async Task<Result> HandleAsync(CardholderChanged e, CancellationToken cancellationToken)
    {
        await store.AcquireProjectionLockAsync(cancellationToken);
        var holder = await store.FindCardholderAsync(e.CardholderId, cancellationToken);
        if (holder is null)
        {
            holder = CardholderState.Create(e.CardholderId);
            store.Add(holder);
        }

        var groupIds = holder.AccessGroupIds.ToHashSet();
        if (!holder.Apply(e.IsActive, e.AccessGroupIds, e.Version, e.OccurredAt))
        {
            return Result.Success();
        }

        groupIds.UnionWith(holder.AccessGroupIds);
        var groups = await store.GetGroupsAsync(groupIds, cancellationToken);
        var sites = groups.Values.SelectMany(g => g.SiteIds).ToHashSet();
        var cards = await store.GetCardsOfCardholdersAsync([e.CardholderId], cancellationToken);
        await reconciler.ReconcileAsync(cards.Select(c => new ReconcileTarget(AccessListEntryKind.Card, c.CardId, sites)).ToList(), e.OccurredAt, cancellationToken);
        return Result.Success();
    }
}
