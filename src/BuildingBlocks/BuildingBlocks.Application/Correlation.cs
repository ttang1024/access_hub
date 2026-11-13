using System.Diagnostics;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>
/// Correlation IDs for integration events. The W3C trace ID is 16 bytes, exactly a GUID, so the event's
/// CorrelationId is the trace ID OpenTelemetry already propagates rather than a hand-rolled ID.
/// </summary>
public static class Correlation
{
    /// <summary>The current trace ID as a GUID, or a new GUID outside a trace.</summary>
    public static Guid Current()
    {
        var activity = Activity.Current;
        if (activity is null || activity.TraceId == default)
        {
            return Guid.NewGuid();
        }

        Span<byte> bytes = stackalloc byte[16];
        activity.TraceId.CopyTo(bytes);
        return new Guid(bytes, bigEndian: true);
    }
}
