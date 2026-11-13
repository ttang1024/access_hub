using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class GetSiteHandler(ICardholderReadModel readModel) : IQueryHandler<GetSiteQuery, SiteDto>
{
    public async Task<Result<SiteDto>> HandleAsync(GetSiteQuery query, CancellationToken cancellationToken) =>
        await readModel.GetSiteAsync(query.SiteId, cancellationToken) is { } site ? site : SiteErrors.NotFound(new SiteId(query.SiteId));
}
