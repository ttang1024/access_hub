using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Simulation;
using AccessHub.SiteSimulator.Storage;
using AccessHub.SiteSimulator.Sync;
using AccessHub.SiteSimulator.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccessHub.SiteSimulator.Tests;

public sealed class OutageDeliveryTests : IDisposable
{
    private readonly TestSiteData _site = new();
    private readonly NetworkConnectivity _connectivity = new();
    private readonly FakeIngestionServer _cloud = new();

    [Fact]
    public async Task AfterOutage_EveryBufferedEventIsDeliveredExactlyOnce_EvenWhenAnAckIsLost()
    {
        _site.SeedSnapshot();
        var holder = new AccessListHolder();
        holder.Replace(SiteSimulator.Sync.ProtoMapper.ToAccessList("Europe/London", _site.Store.LoadEntries()), 4);
        var swipes = new SwipeGenerator(_site.Identity, holder, _site.Store, TestSiteData.Metrics(), TimeProvider.System, new Random(7));
        using var http = new HttpClient(new ConnectivityHandler(_connectivity, TimeProvider.System) { InnerHandler = _cloud }) { BaseAddress = new Uri("http://cloud.test/") };
        var uploader = new EventUploader(_site.Identity, http, _site.Store, TimeProvider.System, NullLogger<EventUploader>.Instance, batchSize: 50);

        // The cloud goes away; the site keeps deciding and buffering.
        _connectivity.StartOutage(TimeSpan.FromHours(1), TimeProvider.System);
        for (var i = 0; i < 120; i++)
        {
            swipes.Tick();
        }

        var generated = _site.Store.PeekEvents(10_000).Select(e => e.EventId).ToHashSet();
        (await uploader.UploadOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(UploadOutcome.Failed);
        _site.Store.BufferedCount().ShouldBe(generated.Count);
        _cloud.StoredCount.ShouldBe(0);

        // The cloud returns, but the acknowledgement of the first batch it stores is lost.
        _connectivity.EndOutage();
        _cloud.DropNextAcknowledgements(1);
        var attempts = 0;
        while (await uploader.UploadOnceAsync(TestContext.Current.CancellationToken) != UploadOutcome.Empty)
        {
            (++attempts).ShouldBeLessThan(1_000);
        }

        _site.Store.BufferedCount().ShouldBe(0);
        _cloud.TimesReceived.Keys.ToHashSet().SetEquals(generated).ShouldBeTrue("every buffered event reached the cloud, and nothing else did");
        _cloud.StoredCount.ShouldBe(generated.Count);
        _cloud.TimesReceived.Values.Count(n => n > 1).ShouldBe(50, "only the batch whose ack was lost was re-sent, and the cloud stored it once");
        _cloud.Batches.ShouldAllBe(batch => batch.SequenceEqual(batch.Order()), "each batch is sent in OccurredAt order");
    }

    [Fact]
    public async Task UploadOnce_CloudRejectsBatchAsInvalid_MovesItAsideSoTheBufferIsNotBlocked()
    {
        using var http = new HttpClient(new RejectingHandler()) { BaseAddress = new Uri("http://cloud.test/") };
        var uploader = new EventUploader(_site.Identity, http, _site.Store, TimeProvider.System, NullLogger<EventUploader>.Instance);
        _site.Store.BufferEvents([new BufferedEvent(Guid.NewGuid(), TestSiteData.Door, "DoorOpened", DateTimeOffset.UtcNow, null, null, null)]);

        (await uploader.UploadOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(UploadOutcome.Sent);

        _site.Store.BufferedCount().ShouldBe(0);
    }

    [Fact]
    public async Task UploadOnce_ResiliencePipelineCircuitOpen_IsAFailedAttemptNotACrash()
    {
        // During an outage the standard resilience pipeline opens its circuit breaker and throws BrokenCircuitException.
        using var http = new HttpClient(new CircuitOpenHandler()) { BaseAddress = new Uri("http://cloud.test/") };
        var uploader = new EventUploader(_site.Identity, http, _site.Store, TimeProvider.System, NullLogger<EventUploader>.Instance);
        _site.Store.BufferEvents([new BufferedEvent(Guid.NewGuid(), TestSiteData.Door, "DoorOpened", DateTimeOffset.UtcNow, null, null, null)]);

        (await uploader.UploadOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(UploadOutcome.Failed);

        _site.Store.BufferedCount().ShouldBe(1);
    }

    public void Dispose() => _site.Dispose();

    private sealed class CircuitOpenHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new Polly.CircuitBreaker.BrokenCircuitException("The circuit is now open and is not allowing calls.");
    }

    private sealed class RejectingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { Content = new StringContent("{\"title\":\"invalid\"}") });
    }
}
