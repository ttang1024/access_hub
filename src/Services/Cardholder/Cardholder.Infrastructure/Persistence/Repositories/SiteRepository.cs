using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Repositories;

internal sealed class SiteRepository(CardholderDbContext db) : ISiteRepository
{
    public Task<Site?> GetAsync(SiteId id, CancellationToken cancellationToken) =>
        db.Sites.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(SiteId id, CancellationToken cancellationToken) =>
        db.Sites.AnyAsync(s => s.Id == id, cancellationToken);

    public void Add(Site site) => db.Sites.Add(site);
}
