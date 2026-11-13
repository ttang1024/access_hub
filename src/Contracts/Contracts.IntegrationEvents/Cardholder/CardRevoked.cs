namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A card was revoked. Carries the full current state of the card.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the card was revoked.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="CardId">Card ID.</param>
/// <param name="CardholderId">Owning cardholder.</param>
/// <param name="CardNumber">Card number.</param>
/// <param name="ExpiresAt">Expiry, or null if the card does not expire.</param>
/// <param name="IsRevoked">Always true.</param>
/// <param name="RevokedAt">When it was revoked.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record CardRevoked(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid CardId, Guid CardholderId, string CardNumber, DateTimeOffset? ExpiresAt, bool IsRevoked, DateTimeOffset? RevokedAt, int Version) : IVersionedStateEvent;
