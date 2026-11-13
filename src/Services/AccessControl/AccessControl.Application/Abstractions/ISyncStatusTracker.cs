namespace AccessHub.AccessControl.Application.Abstractions;

/// <summary>Records per-site stream connection, liveness and delivery progress for the dashboard.</summary>
public interface ISyncStatusTracker
{
    Task StreamOpenedAsync(Guid siteId, CancellationToken cancellationToken);

    Task DeliveredAsync(Guid siteId, long sequence, CancellationToken cancellationToken);

    Task HeartbeatAsync(Guid siteId, CancellationToken cancellationToken);

    Task StreamClosedAsync(Guid siteId);
}
