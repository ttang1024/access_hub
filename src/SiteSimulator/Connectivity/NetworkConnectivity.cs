namespace AccessHub.SiteSimulator.Connectivity;

/// <summary>
/// The site's link to the cloud. An outage fails every new request and cancels open streams; local decisions
/// never depend on it.
/// </summary>
public sealed class NetworkConnectivity
{
    private readonly Lock _gate = new();
    private CancellationTokenSource _online = new();
    private DateTimeOffset _offlineUntil = DateTimeOffset.MinValue;

    public bool IsOnline(TimeProvider clock)
    {
        lock (_gate)
        {
            return clock.GetUtcNow() >= _offlineUntil;
        }
    }

    /// <summary>Cancelled when an outage starts; take a fresh token for each connection.</summary>
    public CancellationToken OnlineToken
    {
        get
        {
            lock (_gate)
            {
                return _online.Token;
            }
        }
    }

    public void StartOutage(TimeSpan duration, TimeProvider clock)
    {
        CancellationTokenSource previous;
        lock (_gate)
        {
            _offlineUntil = clock.GetUtcNow() + duration;
            previous = _online;
            _online = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    public void EndOutage()
    {
        lock (_gate)
        {
            _offlineUntil = DateTimeOffset.MinValue;
        }
    }
}
