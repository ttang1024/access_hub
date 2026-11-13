namespace AccessHub.Gateway;

public sealed class SiteRateLimitOptions
{
    public const string Section = "RateLimiting:Site";

    /// <summary>Sustained requests per second per site.</summary>
    public int PermitsPerSecond { get; set; } = 20;

    /// <summary>Burst a site may use after being idle (for example, draining a buffer after an outage).</summary>
    public int Burst { get; set; } = 60;
}
