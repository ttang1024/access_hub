namespace AccessHub.SiteSimulator;

public sealed class SimulatorOptions
{
    public const string Section = "Simulator";

    public int Sites { get; set; } = 2;

    public int DoorsPerSite { get; set; } = 4;

    public int CardholdersPerSite { get; set; } = 40;

    public string DataDirectory { get; set; } = "simulator-data";

    public TimeSpan SwipeInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>When set, connectivity is cut for this long, starting after <see cref="OutageDelay"/>.</summary>
    public TimeSpan? SimulateOutage { get; set; }

    public TimeSpan OutageDelay { get; set; } = TimeSpan.FromSeconds(30);

    public int UploadBatchSize { get; set; } = 200;

    /// <summary>Gateway base URL for REST (seeding, event upload). Defaults to Aspire service discovery.</summary>
    public Uri? GatewayUrl { get; set; }

    /// <summary>Gateway URL for gRPC (HTTP/2 port).</summary>
    public Uri? GatewayGrpcUrl { get; set; }

    public Uri? TokenEndpoint { get; set; }

    public string ClientId { get; set; } = "site-simulator";

    public string? ClientSecret { get; set; }
}
