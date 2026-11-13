using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.AccessControl.Application.Features.Snapshot;

public sealed class GetSnapshotHandler(ISiteChangeLog changeLog) : IQueryHandler<GetSnapshotQuery, SiteSnapshot>
{
    public async Task<Result<SiteSnapshot>> HandleAsync(GetSnapshotQuery query, CancellationToken cancellationToken) =>
        await changeLog.GetSnapshotAsync(query.SiteId, cancellationToken) is { } snapshot ? snapshot : SyncErrors.UnknownSite(query.SiteId);
}
