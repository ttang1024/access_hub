using System.Diagnostics.Metrics;
using System.Threading.Channels;
using AccessHub.AccessControl.Application;
using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.AccessControl.Domain.AccessLists;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AccessHub.AccessControl.UnitTests.Application;

public sealed class SiteChangeStreamerTests
{
    private static readonly Guid Site = Guid.NewGuid();
    private readonly FakeLog _log = new();
    private readonly FakeFeed _feed = new();

    [Fact]
    public async Task StreamAsync_LiveChangeAfterGap_FillsGapFromLogInOrder()
    {
        _log.Add(1, 2, 3);
        var sent = new List<long>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = Streamer().StreamAsync(Site, 0, Collect(sent, stopAfter: 5, cts), cts.Token);

        _log.Add(4, 5);
        await _feed.WaitForSubscriberAsync();
        _feed.Push(Change(5));

        (await stream).IsSuccess.ShouldBeTrue();
        sent.ShouldBe([1L, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task StreamAsync_LiveChangeAlreadySentFromBacklog_IsSkipped()
    {
        _log.Add(1, 2, 3);
        var sent = new List<long>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = Streamer().StreamAsync(Site, 0, Collect(sent, stopAfter: 4, cts), cts.Token);

        await _feed.WaitForSubscriberAsync();
        _feed.Push(Change(2));
        _feed.Push(Change(3));
        _log.Add(4);
        _feed.Push(Change(4));

        await stream;
        sent.ShouldBe([1L, 2, 3, 4]);
    }

    [Fact]
    public async Task StreamAsync_GapMissingFromLog_FailsInsteadOfSkipping()
    {
        _log.Add(1);
        _log.Position = new ChangeLogPosition(3, 0);
        var sent = new List<long>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = Streamer().StreamAsync(Site, 0, Collect(sent, stopAfter: 10, cts), cts.Token);

        await _feed.WaitForSubscriberAsync();
        _feed.Push(Change(3));

        var result = await stream;
        result.Error!.Code.ShouldBe("Sync.SequenceUnavailable");
        sent.ShouldBe([1L]);
    }

    [Theory]
    [InlineData(2, 10, 5)]  // older than the retained log
    [InlineData(11, 10, 0)] // ahead of the log
    public async Task StreamAsync_FromSequenceOutsideLog_ReturnsSequenceUnavailable(long from, long last, long pruned)
    {
        _log.Position = new ChangeLogPosition(last, pruned);

        var result = await Streamer().StreamAsync(Site, from, (_, _) => Task.CompletedTask, CancellationToken.None);

        result.Error!.Code.ShouldBe("Sync.SequenceUnavailable");
    }

    private static Func<SiteStreamItem, CancellationToken, Task> Collect(List<long> sent, int stopAfter, CancellationTokenSource cts) => (item, _) =>
    {
        if (item.Change is { } change)
        {
            sent.Add(change.Sequence);
            if (sent.Count >= stopAfter)
            {
                cts.Cancel();
            }
        }

        return Task.CompletedTask;
    };

    private static SiteChangeRecord Change(long sequence) => new(Site, sequence, AccessListEntryKind.Card, Guid.NewGuid(), "{}", DateTimeOffset.UtcNow);

    private SiteChangeStreamer Streamer()
    {
        return new SiteChangeStreamer(_log, _feed, Substitute.For<ISyncStatusTracker>(), new AccessControlMetrics(new TestMeterFactory()), Options.Create(new SyncOptions { HeartbeatInterval = TimeSpan.FromMinutes(1) }), TimeProvider.System);
    }

    private sealed class FakeLog : ISiteChangeLog
    {
        private readonly List<SiteChangeRecord> _changes = [];
        private readonly Lock _gate = new();

        public ChangeLogPosition? Position { get; set; }

        public void Add(params long[] sequences)
        {
            lock (_gate)
            {
                _changes.AddRange(sequences.Select(Change));
                Position = new ChangeLogPosition(_changes.Max(c => c.Sequence), 0);
            }
        }

        public Task<SiteSnapshot?> GetSnapshotAsync(Guid siteId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ChangeLogPosition?> GetPositionAsync(Guid siteId, CancellationToken cancellationToken) => Task.FromResult(Position);

        public Task<IReadOnlyList<SiteChangeRecord>> ReadChangesAsync(Guid siteId, long afterSequence, int limit, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult<IReadOnlyList<SiteChangeRecord>>(_changes.Where(c => c.Sequence > afterSequence).OrderBy(c => c.Sequence).Take(limit).ToList());
            }
        }
    }

    private sealed class FakeFeed : ISiteChangeFeed
    {
        private readonly Channel<SiteChangeRecord> _channel = Channel.CreateUnbounded<SiteChangeRecord>();
        private readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ISiteChangeSubscription Subscribe(Guid siteId, long fromSequence)
        {
            _subscribed.TrySetResult();
            return new Subscription(_channel.Reader);
        }

        public Task WaitForSubscriberAsync() => _subscribed.Task;

        public void Push(SiteChangeRecord change) => _channel.Writer.TryWrite(change);

        private sealed class Subscription(ChannelReader<SiteChangeRecord> reader) : ISiteChangeSubscription
        {
            public ChannelReader<SiteChangeRecord> Changes => reader;

            public void Dispose()
            {
            }
        }
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose() => _meters.ForEach(m => m.Dispose());
    }
}
