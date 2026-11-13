using AccessHub.AccessControl.Application;
using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessHub.AccessControl.Infrastructure.Sync;

/// <summary>
/// One per replica: every poll interval, reads newly committed changes for the sites this replica streams to and hands
/// them to the feed. Also refreshes the per-site lag gauge.
/// </summary>
public sealed partial class SiteChangePoller(
    SiteChangeFeed feed,
    IServiceScopeFactory scopeFactory,
    AccessControlMetrics metrics,
    IOptions<SyncOptions> options,
    TimeProvider clock,
    ILogger<SiteChangePoller> logger) : BackgroundService
{
    private const int MaxRowsPerPoll = 5000;
    private static readonly TimeSpan LagRefreshInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, clock);
        var nextLagRefresh = DateTimeOffset.MinValue;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PollAsync(stoppingToken);
                if (clock.GetUtcNow() >= nextLagRefresh)
                {
                    await RefreshLagAsync(stoppingToken);
                    nextLagRefresh = clock.GetUtcNow() + LagRefreshInterval;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Keep polling: a transient database error must not stop delivery for the life of the process.
                LogPollFailed(logger, ex);
            }
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var cursors = feed.Cursors();
        if (cursors.Count == 0)
        {
            return;
        }

        var siteIds = cursors.Select(c => c.SiteId).ToArray();
        var after = cursors.Select(c => c.Cursor).ToArray();
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        var rows = await db.SiteChanges
            .FromSql($"""
                SELECT c.* FROM access_control.site_changes c
                JOIN unnest({siteIds}::uuid[], {after}::bigint[]) AS s(site_id, cursor) ON c.site_id = s.site_id AND c.sequence > s.cursor
                ORDER BY c.site_id, c.sequence
                LIMIT {MaxRowsPerPoll}
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var site in rows.GroupBy(r => r.SiteId))
        {
            feed.Publish(site.Key, site.Select(c => new SiteChangeRecord(c.SiteId, c.Sequence, c.Kind, c.EntityId, c.Payload, c.ChangedAt)).ToList());
        }
    }

    private async Task RefreshLagAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        var lags = await (
                from site in db.Sites.AsNoTracking()
                where site.Version > 0
                join status in db.SyncStatus.AsNoTracking() on site.SiteId equals status.SiteId into statuses
                from status in statuses.DefaultIfEmpty()
                select new { site.SiteId, Lag = site.LastSequence - (status == null ? 0 : status.LastDeliveredSequence) })
            .ToListAsync(cancellationToken);
        foreach (var lag in lags)
        {
            metrics.SetLag(lag.SiteId, Math.Max(lag.Lag, 0));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Polling the site change log failed; retrying on the next tick")]
    private static partial void LogPollFailed(ILogger logger, Exception exception);
}
