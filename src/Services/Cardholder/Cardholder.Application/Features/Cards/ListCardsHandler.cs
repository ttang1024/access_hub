using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class ListCardsHandler(ICardholderReadModel readModel) : IQueryHandler<ListCardsQuery, PagedResult<CardDto>>
{
    public async Task<Result<PagedResult<CardDto>>> HandleAsync(ListCardsQuery query, CancellationToken cancellationToken) =>
        await readModel.ListCardsAsync(query.CardholderId, new PageRequest(query.Page, query.PageSize), cancellationToken);
}
