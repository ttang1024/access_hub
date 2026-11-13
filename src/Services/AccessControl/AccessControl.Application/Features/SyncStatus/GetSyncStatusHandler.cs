using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.AccessControl.Application.Features.SyncStatus;

public sealed class GetSyncStatusHandler(IAccessControlReadModel readModel) : IQueryHandler<GetSyncStatusQuery, IReadOnlyList<SiteSyncStatusDto>>
{
    public async Task<Result<IReadOnlyList<SiteSyncStatusDto>>> HandleAsync(GetSyncStatusQuery query, CancellationToken cancellationToken) =>
        Result.Success(await readModel.GetSyncStatusAsync(cancellationToken));
}
