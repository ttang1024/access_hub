using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AccessHub.Alarm.Infrastructure;

public sealed class AlarmMetrics
{
    public const string MeterName = "AccessHub";
    private readonly ConcurrentDictionary<string, int> _open = new(StringComparer.Ordinal);

    public AlarmMetrics(IMeterFactory meterFactory) =>
        meterFactory.Create(MeterName).CreateObservableGauge(
            "accesshub.alarms.open",
            () => _open.Select(kv => new Measurement<int>(kv.Value, new KeyValuePair<string, object?>("rule", kv.Key))),
            unit: "{alarm}",
            description: "Alarms that are not closed, by rule.");

    public void SetOpen(IReadOnlyDictionary<string, int> byRule)
    {
        foreach (var rule in _open.Keys.Except(byRule.Keys))
        {
            _open[rule] = 0;
        }

        foreach (var (rule, count) in byRule)
        {
            _open[rule] = count;
        }
    }
}
