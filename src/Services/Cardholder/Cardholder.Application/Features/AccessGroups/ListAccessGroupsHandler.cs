using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class ListAccessGroupsHandler(ICardholderReadModel readModel) : IQueryHandler<ListAccessGroupsQuery, PagedResult<AccessGroupDto>>
{
    public async Task<Result<PagedResult<AccessGroupDto>>> HandleAsync(ListAccessGroupsQuery query, CancellationToken cancellationToken) =>
        await readModel.ListAccessGroupsAsync(new PageRequest(query.Page, query.PageSize), cancellationToken);
}
