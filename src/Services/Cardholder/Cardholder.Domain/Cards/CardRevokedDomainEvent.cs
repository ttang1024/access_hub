using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Domain.Cards;

public sealed record CardRevokedDomainEvent(
    CardId CardId, CardholderId CardholderId, string CardNumber, DateTimeOffset? ExpiresAt, DateTimeOffset RevokedAt, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
