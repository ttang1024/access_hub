namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>Kept apart from the sites table so stream bookkeeping never contends with the sequence lock.</summary>
public sealed class SiteSyncStatus
{
    public Guid SiteId { get; set; }

    public bool IsConnected { get; set; }

    public DateTimeOffset? LastConnectedAt { get; set; }

    public DateTimeOffset? LastDisconnectedAt { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public long LastDeliveredSequence { get; set; }
}
