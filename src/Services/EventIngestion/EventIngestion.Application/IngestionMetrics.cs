using System.Diagnostics.Metrics;
using AccessHub.EventIngestion.Domain.Events;

namespace AccessHub.EventIngestion.Application;

public sealed class IngestionMetrics
{
    public const string MeterName = "AccessHub";
    private readonly Counter<long> _ingested;

    public IngestionMetrics(IMeterFactory meterFactory) =>
        _ingested = meterFactory.Create(MeterName).CreateCounter<long>(
            "accesshub.events.ingested", unit: "{event}", description: "Site events received, by type and whether they were new or duplicates.");

    public void Record(DoorEventKind type, bool accepted, long count = 1) =>
        _ingested.Add(count, new KeyValuePair<string, object?>("type", type.ToString()), new KeyValuePair<string, object?>("outcome", accepted ? "accepted" : "duplicate"));
}
