namespace AccessHub.AccessControl.Domain.ReadModel;

public sealed class CardState : VersionedState
{
    private CardState()
    {
    }

    public Guid CardId { get; private set; }

    public Guid CardholderId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static CardState Create(Guid cardId) => new() { CardId = cardId };

    public bool Apply(Guid cardholderId, string number, DateTimeOffset? expiresAt, DateTimeOffset? revokedAt, int version)
    {
        if (!IsNewer(version))
        {
            return false;
        }

        CardholderId = cardholderId;
        Number = number;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        Version = version;
        return true;
    }

    public bool WasRevokedAt(DateTimeOffset timestamp) => RevokedAt is { } revoked && revoked <= timestamp;

    public bool WasExpiredAt(DateTimeOffset timestamp) => ExpiresAt is { } expires && expires <= timestamp;
}
