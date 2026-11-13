using AccessHub.AccessControl.Application.Abstractions;

namespace AccessHub.AccessControl.Application.Sync;

/// <summary>What the stream sends: a change, or a heartbeat carrying the last delivered sequence.</summary>
public sealed record SiteStreamItem(SiteChangeRecord? Change, long? HeartbeatSequence)
{
    public static SiteStreamItem ForChange(SiteChangeRecord change) => new(change, null);

    public static SiteStreamItem Heartbeat(long sequence) => new(null, sequence);
}
