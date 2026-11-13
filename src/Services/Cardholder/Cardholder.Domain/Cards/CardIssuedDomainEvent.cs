using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Domain.Cards;

public sealed record CardIssuedDomainEvent(
    CardId CardId, CardholderId CardholderId, string CardNumber, DateTimeOffset? ExpiresAt, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
