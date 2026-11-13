namespace AccessHub.SiteSimulator.Connectivity;

/// <summary>Exponential backoff with full jitter, so many sites reconnecting after an outage do not stampede.</summary>
public sealed class Backoff(TimeSpan initial, TimeSpan max)
{
    private int _attempt;

    public TimeSpan Next()
    {
        var ceiling = Math.Min(max.TotalMilliseconds, initial.TotalMilliseconds * Math.Pow(2, Math.Min(_attempt, 16)));
        _attempt++;
        return TimeSpan.FromMilliseconds(initial.TotalMilliseconds + (Random.Shared.NextDouble() * (ceiling - initial.TotalMilliseconds)));
    }

    public void Reset() => _attempt = 0;
}
