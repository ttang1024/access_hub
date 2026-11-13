namespace AccessHub.BuildingBlocks.Infrastructure.Auth;

/// <summary>Looks up a site's API key hash from the service's own data (learned from SiteRegistered/SiteApiKeyRotated).</summary>
public interface ISiteApiKeyStore
{
    /// <summary>Returns the SHA-256 hash of the site's current key, or null if the site is unknown.</summary>
    Task<byte[]?> GetKeyHashAsync(Guid siteId, CancellationToken cancellationToken);
}
