using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Application.Abstractions;

public interface ISiteRepository
{
    Task<Site?> GetAsync(SiteId id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(SiteId id, CancellationToken cancellationToken);

    void Add(Site site);
}
