using AccessHub.Alarm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Alarm.Infrastructure.Persistence;

/// <summary>Three statements per batch, however many denials it holds.</summary>
internal sealed class DenialStore(AlarmDbContext db) : IDenialStore
{
    public async Task<IReadOnlyList<StoredDenial>> RecordAsync(Guid siteId, IReadOnlyList<StoredDenial> denials, TimeSpan window, CancellationToken cancellationToken)
    {
        var cards = denials.Select(d => d.CardNumber).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        // Serialise per card, locking in sorted order: concurrent denials of one card must see each other, and
        // two batches sharing cards must not deadlock.
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(k) FROM (SELECT DISTINCT hashtext(c) AS k FROM unnest({cards}::text[]) AS c ORDER BY 1) AS locks",
            cancellationToken);

        var (from, to) = (denials.Min(d => d.OccurredAt) - window, denials.Max(d => d.OccurredAt) + window);
        var existing = await db.RecentDenials.AsNoTracking()
            .Where(d => cards.Contains(d.CardNumber) && d.OccurredAt >= from && d.OccurredAt <= to)
            .Select(d => new StoredDenial(d.SourceEventId, d.CardNumber, d.OccurredAt))
            .ToListAsync(cancellationToken);

        var ids = denials.Select(d => d.SourceEventId).ToArray();
        var numbers = denials.Select(d => d.CardNumber).ToArray();
        var times = denials.Select(d => d.OccurredAt).ToArray();
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO alarm.recent_denials (source_event_id, site_id, card_number, occurred_at)
            SELECT u.id, {siteId}, u.card, u.at FROM unnest({ids}::uuid[], {numbers}::text[], {times}::timestamptz[]) AS u(id, card, at)
            ON CONFLICT DO NOTHING
            """,
            cancellationToken);
        return existing;
    }
}
