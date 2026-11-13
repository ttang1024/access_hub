using System.Net;
using System.Net.Http.Json;
using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.EventIngestion.IntegrationTests;

public sealed class IngestionTests(IngestionFixture fixture)
{
    private static readonly Guid Door = Guid.CreateVersion7();

    [Fact]
    public async Task ReplaySameBatchTwice_StoresEachEventOnceAndPublishesOnce()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();
        var batch = new { events = new[] { Granted(), Denied("CardRevoked"), Simple("DoorForced") } };

        var first = await PostAsync(client, siteId, batch);
        var replay = await PostAsync(client, siteId, batch);

        (first.Accepted, first.Duplicate).ShouldBe((3, 0));
        (replay.Accepted, replay.Duplicate).ShouldBe((0, 3));
        (await fixture.WithDbAsync(db => db.DoorEvents.CountAsync(e => e.SiteId == siteId))).ShouldBe(3);
        foreach (var e in batch.events)
        {
            await fixture.Events.WaitForAsync<DoorEventsRecorded>(b => b.Events.Any(r => r.SourceEventId == e.eventId));
        }

        await Task.Delay(1000, TestContext.Current.CancellationToken);
        fixture.Events.StoredEvents().Count(r => r.SiteId == siteId).ShouldBe(3);
        fixture.Events.All<DoorEventsRecorded>().Count(b => b.SiteId == siteId).ShouldBe(3, "one message per event type in the batch, not one per event");
    }

    [Fact]
    public async Task Publish_DecisionEvent_CarriesCardReasonSequenceAndSiteTime()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();
        var denied = Denied("OutsideSchedule") with { occurredAt = DateTimeOffset.UtcNow.AddHours(-3) };

        await PostAsync(client, siteId, new { events = new[] { denied } });

        await fixture.Events.WaitForAsync<DoorEventsRecorded>(b => b.Events.Any(r => r.SourceEventId == denied.eventId));
        var recorded = fixture.Events.StoredEvents().Single(r => r.SourceEventId == denied.eventId);
        recorded.Type.ShouldBe(DoorEventType.AccessDenied);
        recorded.CardNumber.ShouldBe("CARD42");
        recorded.DenyReason.ShouldBe("OutsideSchedule");
        recorded.AccessListSequence.ShouldBe(17);
        recorded.OccurredAt.ShouldBe(denied.occurredAt, TimeSpan.FromMilliseconds(1));
        recorded.RecordedAt.ShouldBeGreaterThan(recorded.OccurredAt);
    }

    [Fact]
    public async Task FullBatch_PublishesOneMessagePerTypeCarryingEveryEventOnce()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();
        var events = Enumerable.Range(0, 200).Select(i => i % 2 == 0 ? Granted() : Simple("DoorOpened")).ToArray();

        await PostAsync(client, siteId, new { events });

        await fixture.Events.WaitForAsync<DoorEventsRecorded>(b => b.SiteId == siteId && b.Type == DoorEventType.DoorOpened);
        await fixture.Events.WaitForAsync<DoorEventsRecorded>(b => b.SiteId == siteId && b.Type == DoorEventType.AccessGranted);
        var batches = fixture.Events.All<DoorEventsRecorded>().Where(b => b.SiteId == siteId).ToList();
        batches.Count.ShouldBe(2);
        batches.SelectMany(b => b.Events.Select(e => e.SourceEventId)).Order().ShouldBe(events.Select(e => e.eventId).Order());
        batches.ShouldAllBe(b => b.Events.Count == 100);
    }

    [Fact]
    public async Task DuplicateIdWithinOneBatch_StoredOnce()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();
        var e = Simple("DoorOpened");

        var result = await PostAsync(client, siteId, new { events = new[] { e, e } });

        (result.Accepted, result.Duplicate).ShouldBe((1, 1));
    }

    [Fact]
    public async Task OneInvalidEvent_RejectsWholeBatchAndStoresNothing()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", new { events = new[] { Granted(), Denied(null) } }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await fixture.WithDbAsync(db => db.DoorEvents.CountAsync(e => e.SiteId == siteId))).ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task BatchSizeOutOfRange_Returns400(int size)
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", new { events = Enumerable.Range(0, size).Select(_ => Simple("DoorOpened")).ToArray() }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullBatchOf500_IsAccepted()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();

        var result = await PostAsync(client, siteId, new { events = Enumerable.Range(0, 500).Select(_ => Granted()).ToArray() });

        result.Accepted.ShouldBe(500);
    }

    [Fact]
    public async Task EventInFuture_Returns400()
    {
        var (siteId, _, client) = await fixture.RegisterSiteAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", new { events = new[] { Simple("DoorOpened") with { occurredAt = DateTimeOffset.UtcNow.AddHours(1) } } }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostForAnotherSite_Returns403()
    {
        var (_, _, client) = await fixture.RegisterSiteAsync();
        var (otherSite, _, _) = await fixture.RegisterSiteAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/sites/{otherSite}/events", new { events = new[] { Simple("DoorOpened") } }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WrongKeyOrNoCredentials_Returns401()
    {
        var (siteId, _, _) = await fixture.RegisterSiteAsync();
        var wrongKey = fixture.SiteClient(siteId, SiteApiKey.Generate().Plaintext);
        var anonymous = fixture.CreateClient();
        var body = new { events = new[] { Simple("DoorOpened") } };

        (await wrongKey.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", body, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", body, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyRotated_OldKeyRejectedNewKeyAccepted()
    {
        var (siteId, oldKey, oldClient) = await fixture.RegisterSiteAsync();
        var (newKey, newHash) = SiteApiKey.Generate();

        await fixture.Events.PublishAsync(new SiteApiKeyRotated(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), siteId, "S", "UTC", Convert.ToBase64String(newHash), 2));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (await fixture.WithDbAsync(db => db.Sites.Where(s => s.SiteId == siteId).Select(s => s.Version).SingleAsync()) < 2)
        {
            await Task.Delay(50, cts.Token);
        }

        var body = new { events = new[] { Simple("DoorOpened") } };
        (await oldClient.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", body, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await fixture.SiteClient(siteId, newKey).PostAsJsonAsync($"/api/v1/sites/{siteId}/events", body, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        oldKey.ShouldNotBe(newKey);
    }

    private static async Task<IngestResult> PostAsync(HttpClient client, Guid siteId, object body)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/sites/{siteId}/events", body);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IngestResult>())!;
    }

    private static Ev Granted() => new(Guid.CreateVersion7(), Door, "AccessGranted", DateTimeOffset.UtcNow.AddSeconds(-5), "CARD42", null, 17);

    private static Ev Denied(string? reason) => new(Guid.CreateVersion7(), Door, "AccessDenied", DateTimeOffset.UtcNow.AddSeconds(-5), "CARD42", reason, 17);

    private static Ev Simple(string type) => new(Guid.CreateVersion7(), Door, type, DateTimeOffset.UtcNow.AddSeconds(-5), null, null, null);

#pragma warning disable IDE1006, SA1300 // Lower-case members mirror the JSON contract the sites send.
    private sealed record Ev(Guid eventId, Guid doorId, string type, DateTimeOffset occurredAt, string? cardNumber, string? denyReason, long? accessListSequence);
#pragma warning restore IDE1006, SA1300

    private sealed record IngestResult(int Accepted, int Duplicate);
}
