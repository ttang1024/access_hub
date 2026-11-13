using AccessHub.BuildingBlocks.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>Key hashes arrive with SiteRegistered and SiteApiKeyRotated; AccessControl never calls Cardholder.</summary>
internal sealed class SiteApiKeyStore(AccessControlDbContext db) : ISiteApiKeyStore
{
    public Task<byte[]?> GetKeyHashAsync(Guid siteId, CancellationToken cancellationToken) =>
        db.Sites.AsNoTracking().Where(s => s.SiteId == siteId).Select(s => s.ApiKeyHash).SingleOrDefaultAsync(cancellationToken);
}
