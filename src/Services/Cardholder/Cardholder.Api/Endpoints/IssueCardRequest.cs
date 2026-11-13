namespace AccessHub.Cardholder.Api.Endpoints;

public sealed record IssueCardRequest(string CardNumber, DateTimeOffset? ExpiresAt);
