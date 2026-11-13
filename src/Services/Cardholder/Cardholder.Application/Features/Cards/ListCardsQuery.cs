namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed record ListCardsQuery(Guid CardholderId, int Page = 1, int PageSize = 20);
