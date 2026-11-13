namespace AccessHub.EventIngestion.Domain.Sites;

/// <summary>A site's API key hash, learned from SiteRegistered and SiteApiKeyRotated.</summary>
public sealed class KnownSite
{
    private KnownSite()
    {
    }

    public Guid SiteId { get; private set; }

    public byte[] ApiKeyHash { get; private set; } = [];

    public int Version { get; private set; }

    public static KnownSite Create(Guid siteId) => new() { SiteId = siteId };

    public bool Apply(byte[] apiKeyHash, int version)
    {
        if (version <= Version)
        {
            return false;
        }

        ApiKeyHash = apiKeyHash;
        Version = version;
        return true;
    }
}
