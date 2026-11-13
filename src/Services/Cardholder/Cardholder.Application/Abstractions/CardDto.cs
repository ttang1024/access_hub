namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record CardDto(Guid Id, Guid CardholderId, string Number, DateTimeOffset IssuedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? RevokedAt, bool IsRevoked, int Version);
