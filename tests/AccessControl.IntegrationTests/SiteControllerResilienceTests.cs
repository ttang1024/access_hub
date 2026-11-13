extern alias simulator;
using AccessHub.AccessControl.Domain.AccessLists;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging.Abstractions;
using simulator::AccessHub.SiteSimulator;
using simulator::AccessHub.SiteSimulator.Connectivity;
using simulator::AccessHub.SiteSimulator.Storage;
using simulator::AccessHub.SiteSimulator.Sync;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.IntegrationTests;

/// <summary>
/// The real site controller sync worker against the real AccessControl host: outages, reconnect with backoff,
/// re-snapshot, and revocation reaching the site once it is back.
/// </summary>
public sealed class SiteControllerResilienceTests(AccessControlFixture fixture) : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), "accesshub-resilience", Guid.NewGuid().ToString("N"));
    private readonly NetworkConnectivity _connectivity = new();

    [Fact]
    public async Task Outage_SiteKeepsDecidingOnItsCopy_ThenReconnectsAndAppliesTheRevocation()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var (worker, list, _) = CreateWorker(site);
        using var stop = new CancellationTokenSource();
        var running = worker.RunAsync(stop.Token);
        await WaitUntilAsync(() => list.Sequence == 3);

        _connectivity.StartOutage(TimeSpan.FromHours(1), TimeProvider.System);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);

        // Offline: the site still decides, on what it knew (the accepted degradation for offline sites).
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        list.Sequence.ShouldBe(3);
        list.Decide(site.DoorId, number, DateTimeOffset.UtcNow).Decision.Granted.ShouldBeTrue();

        _connectivity.EndOutage();
        await WaitUntilAsync(() => list.Sequence == 4, TimeSpan.FromSeconds(45));
        list.Decide(site.DoorId, number, DateTimeOffset.UtcNow).Decision.Reason.ShouldBe(DenyReason.CardRevoked);

        await stop.CancelAsync();
        await running;
    }

    [Fact]
    public async Task Restart_WithPersistedList_ResumesFromItsSequenceWithoutSnapshot()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var (first, firstList, _) = CreateWorker(site);
        using (var stop = new CancellationTokenSource())
        {
            var running = first.RunAsync(stop.Token);
            await WaitUntilAsync(() => firstList.Sequence == 3);
            await stop.CancelAsync();
            await running;
        }

        var (_, cardHolder2, number2) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 4);

        // Same SQLite file, new process: decides immediately from disk, then catches up from sequence 3.
        var (second, secondList, _) = CreateWorker(site);
        second.LoadPersistedList();
        secondList.Sequence.ShouldBe(3);
        using var stop2 = new CancellationTokenSource();
        var running2 = second.RunAsync(stop2.Token);
        await WaitUntilAsync(() => secondList.Sequence == 4);
        secondList.Decide(site.DoorId, number2, DateTimeOffset.UtcNow).Decision.Granted.ShouldBeTrue();
        cardHolder2.ShouldNotBe(Guid.Empty);

        await stop2.CancelAsync();
        await running2;
    }

    [Fact]
    public async Task SequenceUnknownToCloud_SiteTakesANewSnapshot()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var (worker, list, store) = CreateWorker(site);

        // A local list claiming a sequence the cloud never issued (e.g. the cloud database was restored).
        store.ReplaceSnapshot("UTC", [], 99);
        using var stop = new CancellationTokenSource();
        var running = worker.RunAsync(stop.Token);

        await WaitUntilAsync(() => list.Sequence == 3);
        store.AppliedSequence().ShouldBe(3);
        list.CardNumbers().Count.ShouldBe(1);

        await stop.CancelAsync();
        await running;
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(20));
        while (!condition())
        {
            await Task.Delay(100, cts.Token);
        }
    }

    private (AccessListSyncWorker Worker, AccessListHolder List, SiteStore Store) CreateWorker(TestSite site)
    {
        var channel = GrpcChannel.ForAddress(fixture.Factory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new ConnectivityHandler(_connectivity, TimeProvider.System) { InnerHandler = fixture.Factory.Server.CreateHandler() },
        });
        var identity = new SiteIdentity(site.SiteId, "Resilience", site.ApiKey, [site.DoorId], []);
        var store = new SiteStore(Path.Combine(_dataDirectory, $"{site.SiteId}.db"));
        var list = new AccessListHolder();
        var worker = new AccessListSyncWorker(identity, new Proto.AccessListSync.AccessListSyncClient(channel), store, list, _connectivity, TimeProvider.System, NullLogger<AccessListSyncWorker>.Instance);
        return (worker, list, store);
    }
}
