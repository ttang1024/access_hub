using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Application.Abstractions;

public interface IAccessControlReadModel
{
    Task<IReadOnlyList<SiteSyncStatusDto>> GetSyncStatusAsync(CancellationToken cancellationToken);

    /// <summary>Loads the cloud's copy of a site's list, or null if the site is unknown.</summary>
    Task<(SiteAccessList List, long Sequence)?> LoadAccessListAsync(Guid siteId, CancellationToken cancellationToken);
}
