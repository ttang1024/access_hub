namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed record ListCardholdersQuery(string? Search, int Page = 1, int PageSize = 20);
