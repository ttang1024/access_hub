using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class ListSitesHandler(ICardholderReadModel readModel) : IQueryHandler<ListSitesQuery, PagedResult<SiteDto>>
{
    public async Task<Result<PagedResult<SiteDto>>> HandleAsync(ListSitesQuery query, CancellationToken cancellationToken) =>
        await readModel.ListSitesAsync(new PageRequest(query.Page, query.PageSize), cancellationToken);
}
