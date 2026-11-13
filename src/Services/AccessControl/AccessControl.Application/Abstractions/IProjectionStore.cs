using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;

namespace AccessHub.AccessControl.Application.Abstractions;

/// <summary>
/// Read model and per-site change log, used by the projection handlers inside one transaction.
/// </summary>
public interface IProjectionStore
{
    /// <summary>
    /// Serialises projections. Two Cardholder events touching the same cardholder must not compute site entries from
    /// each other's uncommitted state, so every projection takes one transaction-scoped lock before reading.
    /// </summary>
    Task AcquireProjectionLockAsync(CancellationToken cancellationToken);

    Task<SiteState?> FindSiteAsync(Guid siteId, CancellationToken cancellationToken);

    Task<DoorState?> FindDoorAsync(Guid doorId, CancellationToken cancellationToken);

    Task<AccessGroupState?> FindGroupAsync(Guid accessGroupId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, AccessGroupState>> GetGroupsAsync(IReadOnlyCollection<Guid> accessGroupIds, CancellationToken cancellationToken);

    Task<CardholderState?> FindCardholderAsync(Guid cardholderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetCardholderIdsInGroupAsync(Guid accessGroupId, CancellationToken cancellationToken);

    Task<CardState?> FindCardAsync(Guid cardId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CardState>> GetCardsOfCardholdersAsync(IReadOnlyCollection<Guid> cardholderIds, CancellationToken cancellationToken);

    void Add(SiteState site);

    void Add(DoorState door);

    void Add(AccessGroupState group);

    void Add(CardholderState cardholder);

    void Add(CardState card);

    /// <summary>Sites whose current list holds this entity.</summary>
    Task<IReadOnlyList<Guid>> GetSitesHoldingEntryAsync(AccessListEntryKind kind, Guid entityId, CancellationToken cancellationToken);

    /// <summary>The payload the site currently holds for this entity, or null if it holds none.</summary>
    Task<string?> GetEntryPayloadAsync(Guid siteId, AccessListEntryKind kind, Guid entityId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets (or, with a null payload, removes) the site's entry and appends one change with the next gap-free sequence,
    /// allocated under the site row lock.
    /// </summary>
    Task AppendChangeAsync(Guid siteId, AccessListEntryKind kind, Guid entityId, string? payload, DateTimeOffset changedAt, CancellationToken cancellationToken);

    /// <summary>Flushes pending read-model changes inside the current transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
