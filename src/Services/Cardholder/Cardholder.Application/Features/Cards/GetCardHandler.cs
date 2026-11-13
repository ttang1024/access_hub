using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cards;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class GetCardHandler(ICardholderReadModel readModel) : IQueryHandler<GetCardQuery, CardDto>
{
    public async Task<Result<CardDto>> HandleAsync(GetCardQuery query, CancellationToken cancellationToken) =>
        await readModel.GetCardAsync(query.CardId, cancellationToken) is { } dto ? dto : CardErrors.NotFound(new CardId(query.CardId));
}
