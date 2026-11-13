using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Storage;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.SiteSimulator.Sync;

/// <summary>
/// Keeps the site's list in step with the cloud: a snapshot on first start, then an open change stream. A change is
/// applied only if it is exactly the next sequence, written to SQLite with the new sequence in one transaction; on any
/// gap the stream is reopened from the applied sequence. If the cloud no longer holds that sequence, a new snapshot.
/// </summary>
public sealed partial class AccessListSyncWorker(
    SiteIdentity site,
    Proto.AccessListSync.AccessListSyncClient client,
    SiteStore store,
    AccessListHolder holder,
    NetworkConnectivity connectivity,
    TimeProvider clock,
    ILogger<AccessListSyncWorker> logger)
{
    private readonly Backoff _backoff = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(30));

    public DateTimeOffset? LastHeardFromCloud { get; private set; }

    /// <summary>Loads the persisted list so the site can decide before (or without) reaching the cloud.</summary>
    public void LoadPersistedList()
    {
        if (store.AppliedSequence() is { } sequence && store.TimeZoneId() is { } timeZone)
        {
            holder.Replace(ProtoMapper.ToAccessList(timeZone, store.LoadEntries()), sequence);
            LogLoaded(logger, site.SiteId, sequence);
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        LoadPersistedList();
        while (!stoppingToken.IsCancellationRequested)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, connectivity.OnlineToken);
            try
            {
                if (store.AppliedSequence() is null)
                {
                    await TakeSnapshotAsync(linked.Token);
                }

                await StreamChangesAsync(linked.Token);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
            {
                LogResnapshot(logger, site.SiteId, ex.Status.Detail);
                store.ResetList();
                continue;
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested && ex is RpcException { StatusCode: StatusCode.Cancelled } or OperationCanceledException)
            {
                // Shutting down: the stream was cancelled on purpose and is closed; leave cleanly.
                break;
            }
            catch (Exception ex) when (ex is RpcException or HttpRequestException or IOException or OperationCanceledException)
            {
                LogDisconnected(logger, site.SiteId, ex.Message);
            }

            try
            {
                await Task.Delay(_backoff.Next(), clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down while waiting to reconnect: nothing left to do.
            }
        }
    }

    private async Task TakeSnapshotAsync(CancellationToken cancellationToken)
    {
        var entries = new List<Proto.AccessListEntry>();
        string? timeZone = null;
        long? sequence = null;
        using var call = client.GetSnapshot(new Proto.GetSnapshotRequest(), site.Credentials, cancellationToken: cancellationToken);
        await foreach (var chunk in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            timeZone = chunk.TimeZoneId;
            entries.AddRange(chunk.Entries);
            if (chunk.IsLast)
            {
                sequence = chunk.Sequence;
            }
        }

        if (timeZone is null || sequence is null)
        {
            throw new IOException("Snapshot ended before its last chunk.");
        }

        var list = ProtoMapper.ToAccessList(timeZone, entries);
        store.ReplaceSnapshot(timeZone, entries, sequence.Value);
        holder.Replace(list, sequence.Value);
        LastHeardFromCloud = clock.GetUtcNow();
        LogSnapshot(logger, site.SiteId, entries.Count, sequence.Value);
    }

    private async Task StreamChangesAsync(CancellationToken cancellationToken)
    {
        var applied = store.AppliedSequence() ?? throw new InvalidOperationException("No snapshot to stream from.");
        using var call = client.StreamChanges(new Proto.StreamChangesRequest { FromSequence = applied }, site.Credentials, cancellationToken: cancellationToken);
        LogStreaming(logger, site.SiteId, applied);
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            LastHeardFromCloud = clock.GetUtcNow();
            _backoff.Reset();
            if (message.KindCase != Proto.ChangeStreamMessage.KindOneofCase.Change)
            {
                continue;
            }

            var change = message.Change;
            if (change.Sequence <= applied)
            {
                continue;
            }

            if (change.Sequence != applied + 1 || !store.ApplyChange(change))
            {
                LogGap(logger, site.SiteId, applied, change.Sequence);
                return;
            }

            holder.Apply(change);
            applied = change.Sequence;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Site {SiteId} loaded its persisted access list at sequence {Sequence}")]
    private static partial void LogLoaded(ILogger logger, Guid siteId, long sequence);

    [LoggerMessage(Level = LogLevel.Information, Message = "Site {SiteId} took a snapshot: {Entries} entries at sequence {Sequence}")]
    private static partial void LogSnapshot(ILogger logger, Guid siteId, int entries, long sequence);

    [LoggerMessage(Level = LogLevel.Information, Message = "Site {SiteId} streaming changes after sequence {Sequence}")]
    private static partial void LogStreaming(ILogger logger, Guid siteId, long sequence);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Site {SiteId} saw a gap (applied {Applied}, received {Received}); reconnecting")]
    private static partial void LogGap(ILogger logger, Guid siteId, long applied, long received);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Site {SiteId} must re-snapshot: {Reason}")]
    private static partial void LogResnapshot(ILogger logger, Guid siteId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Site {SiteId} lost its access-list stream ({Reason}); still deciding locally, reconnecting with backoff")]
    private static partial void LogDisconnected(ILogger logger, Guid siteId, string reason);
}
