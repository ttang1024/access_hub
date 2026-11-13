using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.Infrastructure.Persistence;

internal sealed class StaleGrantStore(AccessControlDbContext db) : IStaleGrantStore
{
    public async Task<IReadOnlyDictionary<string, CardState>> FindCardsByNumbersAsync(IReadOnlyCollection<string> cardNumbers, CancellationToken cancellationToken)
    {
        var numbers = cardNumbers.ToList();
        var cards = await db.Cards.AsNoTracking().Where(c => numbers.Contains(c.Number)).ToListAsync(cancellationToken);
        return cards.GroupBy(c => c.Number, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.MaxBy(c => c.Version)!, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyDictionary<Guid, CardholderState>> FindCardholdersAsync(IReadOnlyCollection<Guid> cardholderIds, CancellationToken cancellationToken)
    {
        var ids = cardholderIds.ToList();
        return await db.Cardholders.AsNoTracking().Where(c => ids.Contains(c.CardholderId)).ToDictionaryAsync(c => c.CardholderId, cancellationToken);
    }

    public async Task<bool> TryRecordDetectionAsync(Guid sourceEventId, string reason, DateTimeOffset detectedAt, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO access_control.stale_grant_detections (source_event_id, reason, detected_at) VALUES ({sourceEventId}, {reason}, {detectedAt}) ON CONFLICT DO NOTHING",
            cancellationToken) == 1;
}
