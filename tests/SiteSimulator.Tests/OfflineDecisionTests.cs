using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Simulation;
using AccessHub.SiteSimulator.Storage;
using AccessHub.SiteSimulator.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.SiteSimulator.Tests;

public sealed class OfflineDecisionTests : IDisposable
{
    private readonly TestSiteData _site = new();

    [Fact]
    public void Swipe_AfterRestartWithNoConnectivity_DecidesFromPersistedListAndBuffersEvents()
    {
        _site.SeedSnapshot();

        // A fresh process: new store instance on the same file, the cloud unreachable, the gRPC client never answering.
        var connectivity = new NetworkConnectivity();
        connectivity.StartOutage(TimeSpan.FromHours(1), TimeProvider.System);
        var client = Substitute.For<Proto.AccessListSync.AccessListSyncClient>();
        var store = new SiteStore(_site.DatabasePath);
        var holder = new AccessListHolder();
        new AccessListSyncWorker(_site.Identity, client, store, holder, connectivity, TimeProvider.System, NullLogger<AccessListSyncWorker>.Instance).LoadPersistedList();
        var swipes = new SwipeGenerator(_site.Identity, holder, store, TestSiteData.Metrics(), TimeProvider.System, new Random(1));

        var granted = swipes.Swipe(TestSiteData.Door, TestSiteData.ValidCard);
        var revoked = swipes.Swipe(TestSiteData.Door, TestSiteData.RevokedCard);
        var unknown = swipes.Swipe(TestSiteData.Door, "NOBODY01");
        store.BufferEvents([.. granted, .. revoked, .. unknown]);

        holder.Sequence.ShouldBe(4);
        granted[0].Type.ShouldBe("AccessGranted");
        granted[0].AccessListSequence.ShouldBe(4);
        granted.Select(e => e.Type).ShouldBe(["AccessGranted", "DoorOpened", "DoorClosed"]);
        revoked.ShouldHaveSingleItem().DenyReason.ShouldBe(nameof(DenyReason.CardRevoked));
        unknown.ShouldHaveSingleItem().DenyReason.ShouldBe(nameof(DenyReason.UnknownCard));
        store.BufferedCount().ShouldBe(5);
        client.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void Decide_NeverSynced_FailsClosed()
    {
        var holder = new AccessListHolder();

        holder.Decide(TestSiteData.Door, TestSiteData.ValidCard, DateTimeOffset.UtcNow).Decision.ShouldBe(AccessDecision.Deny(DenyReason.UnknownCard));
    }

    [Fact]
    public void Decide_TenThousandCards_TakesUnderAMillisecond()
    {
        var list = new SiteAccessList("Europe/London");
        var door = Guid.NewGuid();
        var group = Guid.NewGuid();
        list.Upsert(new AccessGroupEntry(group, "G", [new GroupDoorAccess(door, Enum.GetValues<DayOfWeek>().Select(d => new ScheduleWindow(d, 0, 1440)).ToList())]));
        for (var i = 0; i < 10_000; i++)
        {
            list.Upsert(new CardEntry(Guid.NewGuid(), $"C{i:D6}", Guid.NewGuid(), false, null, true, [group]));
        }

        var holder = new AccessListHolder();
        holder.Replace(list, 1);
        var now = DateTimeOffset.UtcNow;
        var timings = new List<TimeSpan>();
        for (var i = 0; i < 2_000; i++)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            holder.Decide(door, $"C{i * 5:D6}", now).Decision.Granted.ShouldBeTrue();
            timings.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));
        }

        timings.Order().ElementAt((int)(timings.Count * 0.99)).ShouldBeLessThan(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void ApplyChange_NotTheNextSequence_IsRejectedAndNothingWritten()
    {
        _site.SeedSnapshot(sequence: 5);
        var gap = new Proto.AccessListChange { Sequence = 7, Kind = Proto.EntityKind.Card, EntityId = Guid.NewGuid().ToString(), Entry = TestSiteData.Card("LATE0001", false) };
        var next = new Proto.AccessListChange { Sequence = 6, Kind = Proto.EntityKind.Card, EntityId = Guid.NewGuid().ToString(), IsTombstone = true };

        _site.Store.ApplyChange(gap).ShouldBeFalse();
        _site.Store.AppliedSequence().ShouldBe(5);
        _site.Store.ApplyChange(next).ShouldBeTrue();
        _site.Store.AppliedSequence().ShouldBe(6);
    }

    [Fact]
    public void Backoff_GrowsExponentiallyWithJitterAndCaps()
    {
        var backoff = new Backoff(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2));

        var delays = Enumerable.Range(0, 12).Select(_ => backoff.Next()).ToList();

        delays.ShouldAllBe(d => d >= TimeSpan.FromMilliseconds(100) && d <= TimeSpan.FromSeconds(2));
        backoff.Reset();
        backoff.Next().ShouldBeLessThanOrEqualTo(TimeSpan.FromMilliseconds(100));
    }

    public void Dispose() => _site.Dispose();
}
