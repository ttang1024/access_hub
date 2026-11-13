using System.Collections.Concurrent;
using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.AccessControl.Infrastructure.Sync;

/// <summary>Writes stream bookkeeping in short scopes of its own, so a long-lived stream never holds a DbContext busy.</summary>
public sealed class SyncStatusTracker(IServiceScopeFactory scopeFactory, TimeProvider clock) : ISyncStatusTracker
{
    private readonly ConcurrentDictionary<Guid, int> _localStreams = new();

    public async Task StreamOpenedAsync(Guid siteId, CancellationToken cancellationToken)
    {
        _localStreams.AddOrUpdate(siteId, 1, (_, n) => n + 1);
        var now = clock.GetUtcNow();
        await ExecuteAsync(
            db => db.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO access_control.site_sync_status (site_id, is_connected, last_connected_at, last_seen_at, last_delivered_sequence)
                VALUES ({siteId}, true, {now}, {now}, 0)
                ON CONFLICT (site_id) DO UPDATE SET is_connected = true, last_connected_at = EXCLUDED.last_connected_at, last_seen_at = EXCLUDED.last_seen_at
                """,
                cancellationToken),
            cancellationToken);
    }

    public Task DeliveredAsync(Guid siteId, long sequence, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return ExecuteAsync(
            db => db.SyncStatus.Where(s => s.SiteId == siteId).ExecuteUpdateAsync(
                s => s.SetProperty(x => x.LastDeliveredSequence, x => x.LastDeliveredSequence > sequence ? x.LastDeliveredSequence : sequence).SetProperty(x => x.LastSeenAt, now),
                cancellationToken),
            cancellationToken);
    }

    public Task HeartbeatAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return ExecuteAsync(
            db => db.SyncStatus.Where(s => s.SiteId == siteId).ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now).SetProperty(x => x.IsConnected, true), cancellationToken),
            cancellationToken);
    }

    public async Task StreamClosedAsync(Guid siteId)
    {
        var remaining = _localStreams.AddOrUpdate(siteId, 0, (_, n) => Math.Max(n - 1, 0));
        if (remaining > 0)
        {
            return;
        }

        var now = clock.GetUtcNow();
        await ExecuteAsync(
            db => db.SyncStatus.Where(s => s.SiteId == siteId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsConnected, false).SetProperty(x => x.LastDisconnectedAt, now), CancellationToken.None),
            CancellationToken.None);
    }

    private async Task ExecuteAsync(Func<AccessControlDbContext, Task<int>> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopeFactory.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AccessControlDbContext>());
    }
}
