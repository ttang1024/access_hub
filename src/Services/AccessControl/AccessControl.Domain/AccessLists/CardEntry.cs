namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>
/// A card as seen by one site. The cardholder's state is denormalised onto the card so a decision needs one lookup;
/// revoked cards and inactive cardholders stay in the list so the deny reason is accurate.
/// </summary>
public sealed record CardEntry(
    Guid CardId, string CardNumber, Guid CardholderId, bool IsRevoked, DateTimeOffset? ExpiresAt, bool CardholderActive, IReadOnlyList<Guid> AccessGroupIds);
