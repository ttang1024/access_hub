using AccessHub.AccessControl.Domain.AccessLists;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.SiteSimulator.Sync;

/// <summary>
/// The in-memory list every swipe is decided against. Decisions and sync updates are serialised with a lock;
/// a decision is a handful of dictionary lookups, so contention is negligible.
/// </summary>
public sealed class AccessListHolder
{
    private readonly Lock _gate = new();
    private SiteAccessList? _list;

    public long Sequence { get; private set; } = -1;

    public bool IsLoaded
    {
        get
        {
            lock (_gate)
            {
                return _list is not null;
            }
        }
    }

    public IReadOnlyList<string> CardNumbers()
    {
        lock (_gate)
        {
            return _list?.Cards.Select(c => c.CardNumber).ToList() ?? [];
        }
    }

    /// <summary>Decides locally. With no list yet (never synced), every card is unknown: fail closed.</summary>
    public (AccessDecision Decision, long Sequence) Decide(Guid doorId, string cardNumber, DateTimeOffset at)
    {
        lock (_gate)
        {
            return _list is null
                ? (AccessDecision.Deny(DenyReason.UnknownCard), Sequence)
                : (AccessDecisionEngine.Decide(_list, doorId, cardNumber, at), Sequence);
        }
    }

    public void Replace(SiteAccessList list, long sequence)
    {
        lock (_gate)
        {
            _list = list;
            Sequence = sequence;
        }
    }

    public void Apply(Proto.AccessListChange change)
    {
        lock (_gate)
        {
            if (_list is null || change.Sequence != Sequence + 1)
            {
                throw new InvalidOperationException($"Change {change.Sequence} does not follow applied sequence {Sequence}.");
            }

            ProtoMapper.Apply(_list, change);
            Sequence = change.Sequence;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _list = null;
            Sequence = -1;
        }
    }
}
