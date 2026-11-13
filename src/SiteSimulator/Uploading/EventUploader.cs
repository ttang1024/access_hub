using System.Net;
using System.Net.Http.Json;
using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Storage;
using Microsoft.Extensions.Logging;

namespace AccessHub.SiteSimulator.Uploading;

/// <summary>
/// Sends buffered events in OccurredAt order and deletes them only after the cloud acknowledges the batch. A retry
/// after a lost acknowledgement is safe: the cloud deduplicates by event ID and reports the repeats as duplicates.
/// </summary>
public sealed partial class EventUploader(
    SiteIdentity site,
    HttpClient http,
    SiteStore store,
    TimeProvider clock,
    ILogger<EventUploader> logger,
    int batchSize = 200)
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>A batch already sent gets this long to be acknowledged during shutdown.</summary>
    private static readonly TimeSpan InFlightGrace = TimeSpan.FromSeconds(30);

    private readonly Backoff _backoff = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(30));

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            UploadOutcome outcome;
            try
            {
                outcome = await UploadOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var delay = outcome switch
            {
                UploadOutcome.Sent => TimeSpan.Zero,
                UploadOutcome.Empty => IdleDelay,
                _ => _backoff.Next(),
            };

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, clock, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Shutting down between batches: events stay buffered for the next start.
                }
            }
        }
    }

    /// <summary>
    /// Uploads one batch. A batch is never cancelled by shutdown once sent: it finishes (or times out after the grace
    /// period) so its outcome is known, and either it is deleted or it stays buffered for the next start.
    /// </summary>
    public async Task<UploadOutcome> UploadOnceAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var batch = store.PeekEvents(batchSize);
        if (batch.Count == 0)
        {
            return UploadOutcome.Empty;
        }

        using var inFlight = new CancellationTokenSource(InFlightGrace);
        try
        {
            using var response = await http.PostAsJsonAsync(
                $"api/v1/sites/{site.SiteId}/events",
                new { events = batch },
                inFlight.Token);
            if (response.IsSuccessStatusCode)
            {
                store.DeleteEvents(batch.Select(e => e.EventId));
                _backoff.Reset();
                LogUploaded(logger, site.SiteId, batch.Count);
                return UploadOutcome.Sent;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // A batch the cloud will never accept must not block the buffer; keep it aside for inspection.
                var reason = await response.Content.ReadAsStringAsync(inFlight.Token);
                store.RejectEvents(batch, reason);
                LogRejected(logger, site.SiteId, batch.Count, reason);
                return UploadOutcome.Sent;
            }

            LogFailed(logger, site.SiteId, $"HTTP {(int)response.StatusCode}");
            return UploadOutcome.Failed;
        }
        // Polly's ExecutionRejectedException covers the resilience pipeline refusing a call (circuit open, timeout):
        // during an outage that is the normal failure, and the loop must keep retrying.
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or Polly.ExecutionRejectedException)
        {
            LogFailed(logger, site.SiteId, ex.Message);
            return UploadOutcome.Failed;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Site {SiteId} uploaded {Count} events")]
    private static partial void LogUploaded(ILogger logger, Guid siteId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Site {SiteId} could not upload events ({Reason}); they stay buffered")]
    private static partial void LogFailed(ILogger logger, Guid siteId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Site {SiteId}: the cloud rejected {Count} events as invalid: {Reason}")]
    private static partial void LogRejected(ILogger logger, Guid siteId, int count, string reason);
}
