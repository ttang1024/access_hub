using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class GetCardholderHandler(ICardholderReadModel readModel) : IQueryHandler<GetCardholderQuery, CardholderDto>
{
    public async Task<Result<CardholderDto>> HandleAsync(GetCardholderQuery query, CancellationToken cancellationToken) =>
        await readModel.GetCardholderAsync(query.CardholderId, cancellationToken) is { } dto ? dto : CardholderErrors.NotFound(new CardholderId(query.CardholderId));
}
