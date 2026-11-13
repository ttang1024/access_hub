using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class GetAccessGroupHandler(ICardholderReadModel readModel) : IQueryHandler<GetAccessGroupQuery, AccessGroupDto>
{
    public async Task<Result<AccessGroupDto>> HandleAsync(GetAccessGroupQuery query, CancellationToken cancellationToken) =>
        await readModel.GetAccessGroupAsync(query.AccessGroupId, cancellationToken) is { } dto ? dto : AccessGroupErrors.NotFound(new AccessGroupId(query.AccessGroupId));
}
