using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed class ListDoorsHandler(ICardholderReadModel readModel) : IQueryHandler<ListDoorsQuery, PagedResult<DoorDto>>
{
    public async Task<Result<PagedResult<DoorDto>>> HandleAsync(ListDoorsQuery query, CancellationToken cancellationToken) =>
        await readModel.ListDoorsAsync(query.SiteId, new PageRequest(query.Page, query.PageSize), cancellationToken);
}
