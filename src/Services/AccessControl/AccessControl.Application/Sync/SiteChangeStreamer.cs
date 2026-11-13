using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.BuildingBlocks.Domain;
using Microsoft.Extensions.Options;

namespace AccessHub.AccessControl.Application.Sync;

/// <summary>
/// Serves <c>StreamChanges</c>: sends every change after the client's sequence, in order and with no gaps, then keeps
/// sending new changes as the poller hands them over, with a heartbeat when idle.
/// It subscribes to the in-process feed <em>before</em> reading the backlog, so a change committed in between is
/// either in the backlog or in the feed; duplicates are skipped and any hole is filled from the log.
/// </summary>
public sealed class SiteChangeStreamer(
    ISiteChangeLog changeLog,
    ISiteChangeFeed feed,
    ISyncStatusTracker status,
    AccessControlMetrics metrics,
    IOptions<SyncOptions> options,
    TimeProvider clock)
{
    private readonly SyncOptions _options = options.Value;

    public async Task<Result> StreamAsync(Guid siteId, long fromSequence, Func<SiteStreamItem, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var position = await changeLog.GetPositionAsync(siteId, cancellationToken);
        if (position is null)
        {
            return SyncErrors.UnknownSite(siteId);
        }

        if (fromSequence < position.PrunedThroughSequence || fromSequence > position.LastSequence)
        {
            return SyncErrors.SequenceUnavailable(fromSequence, position.PrunedThroughSequence, position.LastSequence);
        }

        using var subscription = feed.Subscribe(siteId, fromSequence);
        metrics.StreamOpened(siteId);
        await status.StreamOpenedAsync(siteId, cancellationToken);
        try
        {
            // Asking for changes after N proves the site holds N (for example, straight after a snapshot).
            await status.DeliveredAsync(siteId, fromSequence, cancellationToken);

            var last = await SendFromLogAsync(siteId, fromSequence, long.MaxValue, write, cancellationToken);
            if (last is null)
            {
                return SequenceGone(fromSequence);
            }

            var lastSent = last.Value;
            var nextHeartbeat = clock.GetUtcNow() + _options.HeartbeatInterval;
            while (!cancellationToken.IsCancellationRequested)
            {
                var wait = nextHeartbeat - clock.GetUtcNow();
                if (wait > TimeSpan.Zero && await WaitForChangesAsync(subscription, wait, cancellationToken))
                {
                    var before = lastSent;
                    while (subscription.Changes.TryRead(out var change))
                    {
                        var sent = await SendLiveAsync(siteId, lastSent, change, write, cancellationToken);
                        if (sent is null)
                        {
                            return SequenceGone(lastSent);
                        }

                        lastSent = sent.Value;
                    }

                    if (lastSent != before)
                    {
                        await status.DeliveredAsync(siteId, lastSent, cancellationToken);
                    }

                    continue;
                }

                await write(SiteStreamItem.Heartbeat(lastSent), cancellationToken);
                await status.HeartbeatAsync(siteId, cancellationToken);
                nextHeartbeat = clock.GetUtcNow() + _options.HeartbeatInterval;
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The client went away or the server is shutting down: a normal end of stream.
            return Result.Success();
        }
        finally
        {
            metrics.StreamClosed(siteId);
            await status.StreamClosedAsync(siteId);
        }
    }

    private static Result SequenceGone(long sequence) => SyncErrors.SequenceUnavailable(sequence, sequence, sequence);

    private async Task<bool> WaitForChangesAsync(ISiteChangeSubscription subscription, TimeSpan wait, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(wait, clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await subscription.Changes.WaitToReadAsync(linked.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <returns>The new last-sent sequence, or null if the log no longer holds a change the client needs.</returns>
    private async Task<long?> SendLiveAsync(Guid siteId, long lastSent, SiteChangeRecord change, Func<SiteStreamItem, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        if (change.Sequence <= lastSent)
        {
            return lastSent;
        }

        if (change.Sequence > lastSent + 1)
        {
            var filled = await SendFromLogAsync(siteId, lastSent, change.Sequence - 1, write, cancellationToken);
            if (filled != change.Sequence - 1)
            {
                return null;
            }
        }

        await SendAsync(change, write, cancellationToken);
        return change.Sequence;
    }

    /// <returns>The last sequence sent, or null if the log has a hole (pruned) right after <paramref name="after"/>.</returns>
    private async Task<long?> SendFromLogAsync(Guid siteId, long after, long upTo, Func<SiteStreamItem, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var last = after;
        while (last < upTo)
        {
            var batch = await changeLog.ReadChangesAsync(siteId, last, _options.ReadBatchSize, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var change in batch)
            {
                if (change.Sequence > upTo)
                {
                    return last;
                }

                if (change.Sequence != last + 1)
                {
                    return null;
                }

                await SendAsync(change, write, cancellationToken);
                last = change.Sequence;
            }

            await status.DeliveredAsync(siteId, last, cancellationToken);
        }

        return last;
    }

    private async Task SendAsync(SiteChangeRecord change, Func<SiteStreamItem, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        await write(SiteStreamItem.ForChange(change), cancellationToken);
        metrics.RecordPropagation(clock.GetUtcNow() - change.ChangedAt);
    }
}
