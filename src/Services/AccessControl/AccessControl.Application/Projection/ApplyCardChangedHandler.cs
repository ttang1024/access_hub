using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>Issued and revoked carry the same full card state, so both apply the same way.</summary>
public sealed class ApplyCardChangedHandler(IProjectionStore store, SiteListReconciler reconciler) : ICommandHandler<CardIssued>, ICommandHandler<CardRevoked>
{
    public Task<Result> HandleAsync(CardIssued e, CancellationToken cancellationToken) =>
        ApplyAsync(e.CardId, e.CardholderId, e.CardNumber, e.ExpiresAt, e.IsRevoked ? e.RevokedAt ?? e.OccurredAt : null, e.Version, e.OccurredAt, cancellationToken);

    public Task<Result> HandleAsync(CardRevoked e, CancellationToken cancellationToken) =>
        ApplyAsync(e.CardId, e.CardholderId, e.CardNumber, e.ExpiresAt, e.RevokedAt ?? e.OccurredAt, e.Version, e.OccurredAt, cancellationToken);

    private async Task<Result> ApplyAsync(
        Guid cardId, Guid cardholderId, string number, DateTimeOffset? expiresAt, DateTimeOffset? revokedAt, int version, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        await store.AcquireProjectionLockAsync(cancellationToken);
        var card = await store.FindCardAsync(cardId, cancellationToken);
        if (card is null)
        {
            card = CardState.Create(cardId);
            store.Add(card);
        }

        if (!card.Apply(cardholderId, number, expiresAt, revokedAt, version))
        {
            return Result.Success();
        }

        var holder = await store.FindCardholderAsync(cardholderId, cancellationToken);
        var groups = holder is null ? new Dictionary<Guid, AccessGroupState>() : await store.GetGroupsAsync(holder.AccessGroupIds.ToList(), cancellationToken);
        var sites = groups.Values.SelectMany(g => g.SiteIds).ToHashSet();
        await reconciler.ReconcileAsync([new ReconcileTarget(AccessListEntryKind.Card, cardId, sites)], occurredAt, cancellationToken);
        return Result.Success();
    }
}
