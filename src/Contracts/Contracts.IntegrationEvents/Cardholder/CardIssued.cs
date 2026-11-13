namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A card was issued to a cardholder. Carries the full current state of the card.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the card was issued.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="CardId">Card ID.</param>
/// <param name="CardholderId">Owning cardholder.</param>
/// <param name="CardNumber">Card number as read by readers.</param>
/// <param name="ExpiresAt">Expiry, or null if the card does not expire.</param>
/// <param name="IsRevoked">Whether the card is revoked.</param>
/// <param name="RevokedAt">When it was revoked, if it was.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record CardIssued(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid CardId, Guid CardholderId, string CardNumber, DateTimeOffset? ExpiresAt, bool IsRevoked, DateTimeOffset? RevokedAt, int Version) : IVersionedStateEvent;
