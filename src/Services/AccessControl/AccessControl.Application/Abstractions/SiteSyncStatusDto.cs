namespace AccessHub.AccessControl.Application.Abstractions;

public sealed record SiteSyncStatusDto(
    Guid SiteId, string Name, bool IsConnected, DateTimeOffset? LastConnectedAt, DateTimeOffset? LastSeenAt, long LatestSequence, long LastDeliveredSequence, long Lag);
