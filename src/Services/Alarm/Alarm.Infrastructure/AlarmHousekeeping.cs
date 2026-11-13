using AccessHub.Alarm.Domain;
using AccessHub.Alarm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccessHub.Alarm.Infrastructure;

/// <summary>Refreshes the open-alarms gauge and prunes denials too old to matter for the 60-second rule.</summary>
public sealed partial class AlarmHousekeeping(IServiceScopeFactory scopes, AlarmMetrics metrics, TimeProvider clock, ILogger<AlarmHousekeeping> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    // Out-of-order events can arrive late, so denials are kept well beyond the window.
    private static readonly TimeSpan DenialRetention = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AlarmDbContext>();
                var open = await db.Alarms.AsNoTracking().Where(a => a.CurrentState != AlarmStatus.Closed)
                    .GroupBy(a => a.Rule).Select(g => new { Rule = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Rule, g => g.Count, stoppingToken);
                metrics.SetOpen(open);
                var cutoff = clock.GetUtcNow() - DenialRetention;
                await db.RecentDenials.Where(d => d.OccurredAt < cutoff).ExecuteDeleteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Alarm housekeeping failed; retrying next interval")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
