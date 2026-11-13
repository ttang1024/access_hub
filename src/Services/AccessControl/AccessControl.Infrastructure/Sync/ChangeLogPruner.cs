using AccessHub.AccessControl.Application.Sync;
using AccessHub.AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessHub.AccessControl.Infrastructure.Sync;

/// <summary>
/// Deletes changes older than the retention period and records, per site, the highest pruned sequence. A stream
/// resuming from before that point is told to take a new snapshot.
/// </summary>
public sealed partial class ChangeLogPruner(IServiceScopeFactory scopeFactory, IOptions<SyncOptions> options, TimeProvider clock, ILogger<ChangeLogPruner> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public async Task<int> PruneAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow() - options.Value.Retention;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        return await db.Database.ExecuteSqlAsync(
            $"""
            WITH deleted AS (
                DELETE FROM access_control.site_changes WHERE recorded_at < {cutoff} RETURNING site_id, sequence
            ), pruned AS (
                SELECT site_id, max(sequence) AS through FROM deleted GROUP BY site_id
            )
            UPDATE access_control.sites s SET pruned_through_sequence = GREATEST(s.pruned_through_sequence, p.through)
            FROM pruned p WHERE s.site_id = p.site_id
            """,
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            try
            {
                await PruneAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPruneFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pruning the site change log failed; retrying next interval")]
    private static partial void LogPruneFailed(ILogger logger, Exception exception);
}
