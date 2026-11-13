using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AccessHub.SiteSimulator.Tests;

/// <summary>
/// Behaves like EventIngestion: stores each event ID once and reports duplicates. Can drop the acknowledgement of a
/// batch it has stored, to prove that the client's retry is safe.
/// </summary>
internal sealed class FakeIngestionServer : HttpMessageHandler
{
    private readonly ConcurrentDictionary<Guid, int> _received = new();
    private readonly ConcurrentQueue<List<DateTimeOffset>> _batches = new();
    private int _acksToDrop;

    public IReadOnlyDictionary<Guid, int> TimesReceived => _received;

    public int StoredCount => _received.Count;

    public IReadOnlyCollection<List<DateTimeOffset>> Batches => _batches;

    public void DropNextAcknowledgements(int count) => _acksToDrop = count;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var accepted = 0;
        var times = new List<DateTimeOffset>();
        foreach (var e in body.GetProperty("events").EnumerateArray())
        {
            var id = e.GetProperty("eventId").GetGuid();
            times.Add(e.GetProperty("occurredAt").GetDateTimeOffset());
            if (_received.AddOrUpdate(id, 1, (_, n) => n + 1) == 1)
            {
                accepted++;
            }
        }

        _batches.Enqueue(times);
        if (Interlocked.Decrement(ref _acksToDrop) >= 0)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { accepted, duplicate = times.Count - accepted }) };
    }
}
