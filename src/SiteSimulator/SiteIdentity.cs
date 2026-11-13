using Grpc.Core;

namespace AccessHub.SiteSimulator;

/// <summary>A site controller's identity and credentials (its API key lives on the device).</summary>
public sealed record SiteIdentity(Guid SiteId, string Name, string ApiKey, IReadOnlyList<Guid> DoorIds, IReadOnlyList<string> CardNumbers)
{
    public const string SiteIdHeader = "X-Site-Id";
    public const string ApiKeyHeader = "X-Api-Key";

    public Metadata Credentials => new() { { SiteIdHeader, SiteId.ToString() }, { ApiKeyHeader, ApiKey } };
}
