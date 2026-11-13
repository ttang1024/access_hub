namespace AccessHub.Cardholder.Api.Endpoints;

public sealed record UpdateCardholderRequest(string FirstName, string LastName, bool IsActive, IReadOnlyList<Guid>? AccessGroupIds, int? ExpectedVersion);
