using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class ListCardholdersHandler(ICardholderReadModel readModel) : IQueryHandler<ListCardholdersQuery, PagedResult<CardholderDto>>
{
    public async Task<Result<PagedResult<CardholderDto>>> HandleAsync(ListCardholdersQuery query, CancellationToken cancellationToken) =>
        await readModel.ListCardholdersAsync(query.Search, new PageRequest(query.Page, query.PageSize), cancellationToken);
}
