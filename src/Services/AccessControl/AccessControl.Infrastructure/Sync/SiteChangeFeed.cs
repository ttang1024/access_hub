using System.Threading.Channels;
using AccessHub.AccessControl.Application.Abstractions;

namespace AccessHub.AccessControl.Infrastructure.Sync;

/// <summary>
/// This replica's open streams, grouped by site, with a per-site cursor for the poller. No cross-replica messaging:
/// every replica polls the shared log and feeds only its own streams.
/// </summary>
public sealed class SiteChangeFeed : ISiteChangeFeed
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, SiteSubscribers> _sites = [];

    public ISiteChangeSubscription Subscribe(Guid siteId, long fromSequence)
    {
        var subscription = new Subscription(this, siteId);
        lock (_gate)
        {
            if (!_sites.TryGetValue(siteId, out var subscribers))
            {
                subscribers = new SiteSubscribers(fromSequence);
                _sites[siteId] = subscribers;
            }

            // A late joiner may need older changes than the cursor; re-publishing them is harmless (streams skip duplicates).
            subscribers.Cursor = Math.Min(subscribers.Cursor, fromSequence);
            subscribers.Subscriptions.Add(subscription);
        }

        return subscription;
    }

    /// <summary>Sites with open streams and the sequence after which the poller should read.</summary>
    public IReadOnlyList<(Guid SiteId, long Cursor)> Cursors()
    {
        lock (_gate)
        {
            return _sites.Select(kv => (kv.Key, kv.Value.Cursor)).ToList();
        }
    }

    public void Publish(Guid siteId, IReadOnlyList<SiteChangeRecord> changes)
    {
        if (changes.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            if (!_sites.TryGetValue(siteId, out var subscribers))
            {
                return;
            }

            foreach (var subscription in subscribers.Subscriptions)
            {
                foreach (var change in changes)
                {
                    subscription.Writer.TryWrite(change);
                }
            }

            subscribers.Cursor = Math.Max(subscribers.Cursor, changes[^1].Sequence);
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            if (_sites.TryGetValue(subscription.SiteId, out var subscribers) && subscribers.Subscriptions.Remove(subscription) && subscribers.Subscriptions.Count == 0)
            {
                _sites.Remove(subscription.SiteId);
            }
        }

        subscription.Writer.TryComplete();
    }

    private sealed class SiteSubscribers(long cursor)
    {
        public long Cursor { get; set; } = cursor;

        public List<Subscription> Subscriptions { get; } = [];
    }

    private sealed class Subscription(SiteChangeFeed feed, Guid siteId) : ISiteChangeSubscription
    {
        private readonly Channel<SiteChangeRecord> _channel = Channel.CreateUnbounded<SiteChangeRecord>(new UnboundedChannelOptions { SingleReader = true });

        public Guid SiteId { get; } = siteId;

        public ChannelWriter<SiteChangeRecord> Writer => _channel.Writer;

        public ChannelReader<SiteChangeRecord> Changes => _channel.Reader;

        public void Dispose() => feed.Remove(this);
    }
}
