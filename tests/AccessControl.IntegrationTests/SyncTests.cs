using System.Diagnostics;
using AccessHub.AccessControl.Infrastructure.Sync;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.IntegrationTests;

public sealed class SyncTests(AccessControlFixture fixture)
{
    private readonly Proto.AccessListSync.AccessListSyncClient _client = fixture.SyncClient();

    [Fact]
    public async Task GetSnapshot_WithNoCardholderServiceRunning_ServesListFromOwnReadModel()
    {
        // Only AccessControl is running in this test host: the list is served from its own read model.
        var site = await new TestSite(fixture).RegisterAsync("Europe/Berlin");
        var (_, cardId, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);

        var snapshot = await LocalAccessList.SnapshotAsync(_client, site.Credentials);

        snapshot.TimeZoneId.ShouldBe("Europe/Berlin");
        snapshot.Sequence.ShouldBe(3);
        snapshot.Entries.Keys.Select(k => k.Item1).Order().ShouldBe([Proto.EntityKind.Door, Proto.EntityKind.AccessGroup, Proto.EntityKind.Card]);
        var card = snapshot.Entries[(Proto.EntityKind.Card, cardId.ToString())].Card;
        card.CardNumber.ShouldBe(number);
        card.CardholderActive.ShouldBeTrue();
        card.AccessGroupIds.ShouldBe([site.GroupId.ToString()]);
    }

    [Fact]
    public async Task SnapshotPlusStream_AfterMoreChanges_EqualsCloudList()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var local = await LocalAccessList.SnapshotAsync(_client, site.Credentials);
        await using var stream = new ChangeStream(_client, site.Credentials, local.Sequence);

        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number));
        await fixture.Events.PublishAsync(site.GroupEvent(new Contracts.IntegrationEvents.Cardholder.WeeklyTimeWindow(DayOfWeek.Monday, 480, 1020)));
        // New card, its revocation, the group's new schedule. The schedule change does not alter card entries
        // (cards list group IDs, not windows), so it appends one change, not one per card.
        await fixture.WaitForSequenceAsync(site.SiteId, 6);
        foreach (var change in await stream.TakeChangesAsync(3))
        {
            local.Apply(change);
        }

        local.ShouldEqual(await LocalAccessList.SnapshotAsync(_client, site.Credentials));
    }

    [Fact]
    public async Task StreamChanges_ResumingFromOldSequence_ReceivesExactlyTheMissedChangesInOrder()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        for (var i = 0; i < 5; i++)
        {
            await site.AddCardAsync();
        }

        await fixture.WaitForSequenceAsync(site.SiteId, 7);

        await using var stream = new ChangeStream(_client, site.Credentials, fromSequence: 3);
        var changes = await stream.TakeChangesAsync(4);

        changes.Select(c => c.Sequence).ShouldBe([4L, 5L, 6L, 7L]);
        changes.ShouldAllBe(c => c.Kind == Proto.EntityKind.Card && !c.IsTombstone);
        await Should.ThrowAsync<OperationCanceledException>(() => stream.NextChangeAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ConcurrentWriters_ManyEventsAtOnce_ProduceNoGapAndStreamSkipsNothing()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await using var stream = new ChangeStream(_client, site.Credentials, fromSequence: 2);
        const int Cards = 40;

        await Task.WhenAll(Enumerable.Range(0, Cards).Select(_ => site.AddCardAsync()));
        await fixture.WaitForSequenceAsync(site.SiteId, 2 + Cards);

        var sequences = await fixture.WithDbAsync(db =>
            db.SiteChanges.AsNoTracking().Where(c => c.SiteId == site.SiteId).OrderBy(c => c.Sequence).Select(c => c.Sequence).ToListAsync());
        sequences.ShouldBe(Enumerable.Range(1, 2 + Cards).Select(i => (long)i));
        var streamed = await stream.TakeChangesAsync(Cards);
        streamed.Select(c => c.Sequence).ShouldBe(Enumerable.Range(3, Cards).Select(i => (long)i));
    }

    [Fact]
    public async Task StreamChanges_FromSequenceOlderThanRetainedLog_FailsWithFailedPrecondition()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        await fixture.WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE access_control.site_changes SET recorded_at = now() - interval '8 days' WHERE site_id = {site.SiteId} AND sequence <= 2"));
        await fixture.Factory.Services.GetRequiredService<ChangeLogPruner>().PruneAsync(TestContext.Current.CancellationToken);

        await using var expired = new ChangeStream(_client, site.Credentials, fromSequence: 1);
        (await expired.FailureAsync()).StatusCode.ShouldBe(StatusCode.FailedPrecondition);

        await using var fine = new ChangeStream(_client, site.Credentials, fromSequence: 2);
        (await fine.NextChangeAsync()).Sequence.ShouldBe(3);
    }

    [Fact]
    public async Task StreamChanges_FromSequenceAheadOfLog_FailsWithFailedPrecondition()
    {
        var site = await new TestSite(fixture).RegisterAsync();

        await using var stream = new ChangeStream(_client, site.Credentials, fromSequence: 99);

        (await stream.FailureAsync()).StatusCode.ShouldBe(StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task Revocation_OnOpenStream_ArrivesWithinFiveSeconds()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        await using var stream = new ChangeStream(_client, site.Credentials, fromSequence: 3);

        var stopwatch = Stopwatch.StartNew();
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number));
        var change = await stream.NextChangeAsync(TimeSpan.FromSeconds(5));
        stopwatch.Stop();

        change.EntityId.ShouldBe(card.ToString());
        change.Entry.Card.IsRevoked.ShouldBeTrue();
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StreamChanges_WhenIdle_SendsHeartbeatsAndReportsSiteConnected()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        await using var stream = new ChangeStream(_client, site.Credentials, fromSequence: 0);
        await stream.TakeChangesAsync(2);

        var heartbeat = await stream.NextHeartbeatAsync(AccessControlFixture.HeartbeatInterval * 3);

        heartbeat.LatestSequence.ShouldBe(2);
        var status = await fixture.WithDbAsync(db => db.SyncStatus.AsNoTracking().SingleAsync(s => s.SiteId == site.SiteId));
        status.IsConnected.ShouldBeTrue();
        status.LastDeliveredSequence.ShouldBe(2);
    }

    [Fact]
    public async Task StreamChanges_OpenedAtLatestSequenceAfterSnapshot_ReportsNoLag()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var snapshot = await LocalAccessList.SnapshotAsync(_client, site.Credentials);

        // Nothing to send: the site is current as soon as it opens the stream at the snapshot's sequence.
        await using var stream = new ChangeStream(_client, site.Credentials, snapshot.Sequence);
        await stream.NextHeartbeatAsync(AccessControlFixture.HeartbeatInterval * 3);

        var status = await fixture.WithDbAsync(db => db.SyncStatus.AsNoTracking().SingleAsync(s => s.SiteId == site.SiteId));
        status.LastDeliveredSequence.ShouldBe(snapshot.Sequence);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetSnapshot_WithBadCredentials_IsUnauthenticated(bool knownSite)
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var credentials = new Metadata
        {
            { "X-Site-Id", knownSite ? site.SiteId.ToString() : Guid.NewGuid().ToString() },
            { "X-Api-Key", BuildingBlocks.Application.SiteApiKey.Generate().Plaintext },
        };

        var error = await Should.ThrowAsync<RpcException>(() => LocalAccessList.SnapshotAsync(_client, credentials));

        error.StatusCode.ShouldBe(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task GetSnapshot_TwoSites_EachSeesOnlyItsOwnList()
    {
        var a = await new TestSite(fixture).RegisterAsync();
        var b = await new TestSite(fixture).RegisterAsync();

        var listA = await LocalAccessList.SnapshotAsync(_client, a.Credentials);

        listA.Entries.Keys.ShouldContain((Proto.EntityKind.Door, a.DoorId.ToString()));
        listA.Entries.Keys.ShouldNotContain((Proto.EntityKind.Door, b.DoorId.ToString()));
    }
}
