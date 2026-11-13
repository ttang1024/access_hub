using System.Threading.RateLimiting;

namespace AccessHub.Gateway;

/// <summary>Site endpoints are rate-limited per site, partitioned by the X-Site-Id header.</summary>
internal static class SiteRateLimiting
{
    public const string Policy = "site";
    private const string SiteIdHeader = "X-Site-Id";
    private const string InvalidPartition = "invalid-site-id";

    public static IServiceCollection AddSiteRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SiteRateLimitOptions.Section).Get<SiteRateLimitOptions>() ?? new SiteRateLimitOptions();
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Policy, context =>
            {
                // Only well-formed site IDs get their own partition, so random header values cannot create
                // unbounded partitions; malformed ones share one tight bucket (the services reject them anyway).
                var header = context.Request.Headers[SiteIdHeader].ToString();
                return Guid.TryParse(header, out var siteId)
                    ? RateLimitPartition.GetTokenBucketLimiter(siteId.ToString(), _ => Bucket(options.PermitsPerSecond, options.Burst))
                    : RateLimitPartition.GetTokenBucketLimiter(InvalidPartition, _ => Bucket(1, 5));
            });
        });
    }

    private static TokenBucketRateLimiterOptions Bucket(int perSecond, int burst) => new()
    {
        TokenLimit = burst,
        TokensPerPeriod = perSecond,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        QueueLimit = 0,
        AutoReplenishment = true,
    };
}
