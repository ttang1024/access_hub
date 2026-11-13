namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed record CreateCardholderCommand(string FirstName, string LastName, IReadOnlyList<Guid>? AccessGroupIds);
