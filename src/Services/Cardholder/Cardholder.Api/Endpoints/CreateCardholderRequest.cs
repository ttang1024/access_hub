namespace AccessHub.Cardholder.Api.Endpoints;

public sealed record CreateCardholderRequest(string FirstName, string LastName, IReadOnlyList<Guid>? AccessGroupIds);
