using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace AccessHub.EventIngestion.Infrastructure.Persistence;

/// <summary>
/// One multi-row insert per batch. <c>ON CONFLICT DO NOTHING ... RETURNING</c> reports exactly the rows that were new,
/// which are the only ones published. Runs on the context's connection and transaction so it commits with the outbox.
/// </summary>
internal sealed class DoorEventStore(IngestionDbContext db) : IDoorEventStore
{
    private const string InsertSql = """
        INSERT INTO ingestion.door_events (site_id, event_id, door_id, type, occurred_at, card_number, deny_reason, access_list_sequence, recorded_at)
        SELECT @site_id, u.event_id, u.door_id, u.type, u.occurred_at, u.card_number, u.deny_reason, u.access_list_sequence, @recorded_at
        FROM unnest(@event_ids, @door_ids, @types, @occurred_ats, @card_numbers, @deny_reasons, @sequences)
            AS u(event_id, door_id, type, occurred_at, card_number, deny_reason, access_list_sequence)
        ON CONFLICT (site_id, event_id) DO NOTHING
        RETURNING event_id
        """;

    public async Task<IReadOnlyList<DoorEvent>> InsertNewAsync(IReadOnlyList<DoorEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return [];
        }

        var transaction = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Events must be inserted inside the unit of work's transaction.");
        await using var command = new NpgsqlCommand(InsertSql, (NpgsqlConnection)transaction.Connection!, (NpgsqlTransaction)transaction);
        command.Parameters.AddWithValue("site_id", events[0].SiteId);
        command.Parameters.AddWithValue("recorded_at", events[0].RecordedAt);
        command.Parameters.AddWithValue("event_ids", events.Select(e => e.EventId).ToArray());
        command.Parameters.AddWithValue("door_ids", events.Select(e => e.DoorId).ToArray());
        command.Parameters.AddWithValue("types", events.Select(e => (int)e.Type).ToArray());
        command.Parameters.AddWithValue("occurred_ats", events.Select(e => e.OccurredAt).ToArray());
        command.Parameters.AddWithValue("card_numbers", events.Select(e => e.CardNumber).ToArray());
        command.Parameters.AddWithValue("deny_reasons", events.Select(e => e.DenyReason).ToArray());
        command.Parameters.AddWithValue("sequences", events.Select(e => e.AccessListSequence).ToArray());

        var inserted = new HashSet<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                inserted.Add(reader.GetGuid(0));
            }
        }

        // A batch may repeat an event ID; it is stored (and published) once.
        var seen = new HashSet<Guid>();
        return events.Where(e => inserted.Contains(e.EventId) && seen.Add(e.EventId)).ToList();
    }
}
