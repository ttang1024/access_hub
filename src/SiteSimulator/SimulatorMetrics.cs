using System.Diagnostics.Metrics;
using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.SiteSimulator;

public sealed class SimulatorMetrics
{
    public const string MeterName = "AccessHub";
    private readonly Counter<long> _decisions;
    private readonly Histogram<double> _decisionDuration;

    public SimulatorMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _decisions = meter.CreateCounter<long>("accesshub.access_decisions", unit: "{decision}", description: "Local access decisions, by result and reason.");
        _decisionDuration = meter.CreateHistogram(
            "accesshub.access_decision.duration",
            unit: "s",
            description: "Time to decide a swipe locally.",
            tags: null,
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.00001, 0.000025, 0.00005, 0.0001, 0.00025, 0.0005, 0.001, 0.0025, 0.005, 0.01] });
    }

    public void RecordDecision(AccessDecision decision, TimeSpan duration, Guid siteId)
    {
        var site = new KeyValuePair<string, object?>("site_id", siteId.ToString());
        _decisions.Add(
            1,
            new KeyValuePair<string, object?>("result", decision.Granted ? "granted" : "denied"),
            new KeyValuePair<string, object?>("reason", decision.Reason?.ToString() ?? "none"),
            site);
        _decisionDuration.Record(duration.TotalSeconds, site);
    }
}
