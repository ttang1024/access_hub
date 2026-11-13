using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AccessHub.AccessControl.Application;

/// <summary>AccessControl's custom metrics.</summary>
public sealed class AccessControlMetrics
{
    public const string MeterName = "AccessHub";

    private readonly Histogram<double> _propagation;
    private readonly ConcurrentDictionary<Guid, int> _connected = new();
    private readonly ConcurrentDictionary<Guid, long> _lag = new();

    public AccessControlMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _propagation = meter.CreateHistogram<double>(
            "accesshub.access_list.propagation.duration",
            unit: "s",
            description: "From the Cardholder event's OccurredAt to the change being written to a site's stream.");
        meter.CreateObservableGauge(
            "accesshub.sites.connected",
            () => _connected.Count(kv => kv.Value > 0),
            unit: "{site}",
            description: "Sites with an open change stream on this replica.");
        meter.CreateObservableGauge(
            "accesshub.sites.lag",
            () => _lag.Select(kv => new Measurement<long>(kv.Value, new KeyValuePair<string, object?>("site_id", kv.Key.ToString()))),
            unit: "{change}",
            description: "Changes appended to a site's log but not yet delivered to it.");
    }

    public void RecordPropagation(TimeSpan duration) => _propagation.Record(Math.Max(duration.TotalSeconds, 0));

    public void StreamOpened(Guid siteId) => _connected.AddOrUpdate(siteId, 1, (_, n) => n + 1);

    public void StreamClosed(Guid siteId) => _connected.AddOrUpdate(siteId, 0, (_, n) => Math.Max(n - 1, 0));

    public void SetLag(Guid siteId, long lag) => _lag[siteId] = lag;
}
