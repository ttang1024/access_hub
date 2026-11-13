namespace AccessHub.AccessControl.Domain.ReadModel;

/// <summary>
/// Decides whether a grant a site made was stale: the cloud already knew, at the grant's time, that the card was
/// revoked or expired or the cardholder inactive. Known limitation: if the grant is processed before the revocation
/// event reaches AccessControl, the cloud does not know yet and the grant is not flagged.
/// </summary>
public static class StaleGrantDetector
{
    public static string? Check(CardState? card, CardholderState? cardholder, DateTimeOffset grantedAt)
    {
        if (card is null)
        {
            return null;
        }

        if (card.WasRevokedAt(grantedAt))
        {
            return "CardRevoked";
        }

        if (card.WasExpiredAt(grantedAt))
        {
            return "CardExpired";
        }

        return cardholder is not null && cardholder.CardholderId == card.CardholderId && cardholder.WasInactiveAt(grantedAt) ? "CardholderInactive" : null;
    }
}
