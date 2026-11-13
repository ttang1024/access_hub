namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed record UpdateCardholderCommand(Guid CardholderId, string FirstName, string LastName, bool IsActive, IReadOnlyList<Guid>? AccessGroupIds, int? ExpectedVersion);
