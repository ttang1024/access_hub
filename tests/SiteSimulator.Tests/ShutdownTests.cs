using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Simulation;
using AccessHub.SiteSimulator.Sync;
using AccessHub.SiteSimulator.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccessHub.SiteSimulator.Tests;

public sealed class ShutdownTests : IDisposable
{
    private readonly TestSiteData _site = new();

    [Fact]
    public async Task UploaderAndSwipes_StoppedDuringOutage_EndWithoutErrorsAndKeepEventsBuffered()
    {
        _site.SeedSnapshot();
        var connectivity = new NetworkConnectivity();
        connectivity.StartOutage(TimeSpan.FromHours(1), TimeProvider.System);
        var holder = new AccessListHolder();
        holder.Replace(Sync.ProtoMapper.ToAccessList("UTC", _site.Store.LoadEntries()), 4);
        using var http = new HttpClient(new ConnectivityHandler(connectivity, TimeProvider.System) { InnerHandler = new FakeIngestionServer() }) { BaseAddress = new Uri("http://cloud.test/") };
        var uploader = new EventUploader(_site.Identity, http, _site.Store, TimeProvider.System, NullLogger<EventUploader>.Instance);
        var swipes = new SwipeGenerator(_site.Identity, holder, _site.Store, TestSiteData.Metrics(), TimeProvider.System, new Random(3));
        using var stop = new CancellationTokenSource();

        var running = Task.WhenAll(uploader.RunAsync(stop.Token), swipes.RunAsync(TimeSpan.FromMilliseconds(20), stop.Token));
        await Task.Delay(500, TestContext.Current.CancellationToken);
        await stop.CancelAsync();

        await Should.NotThrowAsync(() => running.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        _site.Store.BufferedCount().ShouldBeGreaterThan(0);
    }

    public void Dispose() => _site.Dispose();
}
