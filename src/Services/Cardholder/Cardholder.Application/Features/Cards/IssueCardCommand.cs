namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed record IssueCardCommand(Guid CardholderId, string CardNumber, DateTimeOffset? ExpiresAt);
