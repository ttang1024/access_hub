using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.EventIngestion.Infrastructure.Persistence;

internal sealed class KnownSiteStore(IngestionDbContext db) : IKnownSiteStore, ISiteApiKeyStore
{
    public async Task<KnownSite?> FindAsync(Guid siteId, CancellationToken cancellationToken) => await db.Sites.FindAsync([siteId], cancellationToken);

    public void Add(KnownSite site) => db.Sites.Add(site);

    public async Task<byte[]?> GetKeyHashAsync(Guid siteId, CancellationToken cancellationToken) =>
        await db.Sites.AsNoTracking().Where(s => s.SiteId == siteId).Select(s => s.ApiKeyHash).SingleOrDefaultAsync(cancellationToken);
}
