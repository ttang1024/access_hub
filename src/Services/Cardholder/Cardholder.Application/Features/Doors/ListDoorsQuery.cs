namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed record ListDoorsQuery(Guid SiteId, int Page = 1, int PageSize = 20);
