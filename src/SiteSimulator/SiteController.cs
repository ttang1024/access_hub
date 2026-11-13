using AccessHub.SiteSimulator.Simulation;
using AccessHub.SiteSimulator.Storage;
using AccessHub.SiteSimulator.Sync;
using AccessHub.SiteSimulator.Uploading;

namespace AccessHub.SiteSimulator;

/// <summary>One site controller: keeps its list in sync, decides every swipe locally, buffers and uploads events.</summary>
public sealed class SiteController(SiteIdentity site, SiteStore store, AccessListHolder holder, AccessListSyncWorker sync, EventUploader uploader, SwipeGenerator swipes)
{
    public SiteIdentity Site => site;

    public SiteStore Store => store;

    public AccessListHolder AccessList => holder;

    public Task RunAsync(TimeSpan swipeInterval, CancellationToken stoppingToken) =>
        Task.WhenAll(sync.RunAsync(stoppingToken), uploader.RunAsync(stoppingToken), RunSwipesAsync(swipeInterval, stoppingToken));

    private async Task RunSwipesAsync(TimeSpan interval, CancellationToken stoppingToken)
    {
        // Give the first sync a moment so the demo starts with real decisions; a site that cannot reach the cloud
        // still starts deciding (from its persisted list, or failing closed if it has never synced).
        for (var i = 0; i < 30 && !holder.IsLoaded && !stoppingToken.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        await swipes.RunAsync(interval, stoppingToken);
    }
}
