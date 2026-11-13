using AccessHub.EventIngestion.Domain.Sites;

namespace AccessHub.EventIngestion.Application.Abstractions;

public interface IKnownSiteStore
{
    Task<KnownSite?> FindAsync(Guid siteId, CancellationToken cancellationToken);

    void Add(KnownSite site);
}
