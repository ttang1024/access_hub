using System.Globalization;
using AccessHub.Contracts.Grpc.Sync.V1;
using Google.Protobuf;
using Microsoft.Data.Sqlite;

namespace AccessHub.SiteSimulator.Storage;

/// <summary>
/// The site controller's SQLite database: its copy of the access list (with the applied sequence) and the event buffer.
/// Every change is written with its sequence in one transaction, so a crash never leaves the two out of step.
/// </summary>
public sealed class SiteStore
{
    private readonly string _connectionString;

    public SiteStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = true }.ToString();
        using var connection = Open();
        Execute(connection, null, """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS entries (kind INTEGER NOT NULL, entity_id TEXT NOT NULL, payload BLOB NOT NULL, PRIMARY KEY (kind, entity_id));
            CREATE TABLE IF NOT EXISTS events (
                event_id TEXT PRIMARY KEY, occurred_at INTEGER NOT NULL, door_id TEXT NOT NULL, type TEXT NOT NULL,
                card_number TEXT NULL, deny_reason TEXT NULL, sequence INTEGER NULL);
            CREATE INDEX IF NOT EXISTS ix_events_occurred_at ON events (occurred_at);
            CREATE TABLE IF NOT EXISTS rejected_events (event_id TEXT PRIMARY KEY, body TEXT NOT NULL, reason TEXT NOT NULL);
            """);
    }

    /// <summary>The applied sequence, or null if no snapshot has been taken yet.</summary>
    public long? AppliedSequence() => long.TryParse(GetMeta("applied_sequence"), CultureInfo.InvariantCulture, out var seq) ? seq : null;

    public string? TimeZoneId() => GetMeta("time_zone_id");

    public IReadOnlyList<AccessListEntry> LoadEntries()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM entries";
        using var reader = command.ExecuteReader();
        var entries = new List<AccessListEntry>();
        while (reader.Read())
        {
            entries.Add(AccessListEntry.Parser.ParseFrom((byte[])reader[0]));
        }

        return entries;
    }

    public void ReplaceSnapshot(string timeZoneId, IReadOnlyCollection<AccessListEntry> entries, long sequence)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        Execute(connection, transaction, "DELETE FROM entries");
        foreach (var entry in entries)
        {
            Upsert(connection, transaction, entry);
        }

        SetMeta(connection, transaction, "time_zone_id", timeZoneId);
        SetMeta(connection, transaction, "applied_sequence", sequence.ToString(CultureInfo.InvariantCulture));
        transaction.Commit();
    }

    /// <summary>Applies a change only if it is exactly the next sequence. Returns false (and writes nothing) otherwise.</summary>
    public bool ApplyChange(AccessListChange change)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var applied = long.Parse(GetMeta(connection, transaction, "applied_sequence") ?? "-1", CultureInfo.InvariantCulture);
        if (change.Sequence != applied + 1)
        {
            return false;
        }

        if (change.IsTombstone)
        {
            using var delete = Command(connection, transaction, "DELETE FROM entries WHERE kind = $kind AND entity_id = $id");
            delete.Parameters.AddWithValue("$kind", (int)change.Kind);
            delete.Parameters.AddWithValue("$id", change.EntityId);
            delete.ExecuteNonQuery();
        }
        else
        {
            Upsert(connection, transaction, change.Entry);
        }

        SetMeta(connection, transaction, "applied_sequence", change.Sequence.ToString(CultureInfo.InvariantCulture));
        transaction.Commit();
        return true;
    }

    /// <summary>Forgets the list so the next sync takes a fresh snapshot (the cloud no longer holds our sequence).</summary>
    public void ResetList()
    {
        using var connection = Open();
        Execute(connection, null, "DELETE FROM entries; DELETE FROM meta WHERE key IN ('applied_sequence', 'time_zone_id');");
    }

    /// <summary>Writes events to the buffer before anything else happens to them.</summary>
    public void BufferEvents(IReadOnlyCollection<BufferedEvent> events)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var e in events)
        {
            using var insert = Command(connection, transaction, """
                INSERT OR IGNORE INTO events (event_id, occurred_at, door_id, type, card_number, deny_reason, sequence)
                VALUES ($id, $at, $door, $type, $card, $reason, $seq)
                """);
            insert.Parameters.AddWithValue("$id", e.EventId.ToString());
            insert.Parameters.AddWithValue("$at", e.OccurredAt.UtcTicks);
            insert.Parameters.AddWithValue("$door", e.DoorId.ToString());
            insert.Parameters.AddWithValue("$type", e.Type);
            insert.Parameters.AddWithValue("$card", (object?)e.CardNumber ?? DBNull.Value);
            insert.Parameters.AddWithValue("$reason", (object?)e.DenyReason ?? DBNull.Value);
            insert.Parameters.AddWithValue("$seq", (object?)e.AccessListSequence ?? DBNull.Value);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>The oldest buffered events by OccurredAt.</summary>
    public IReadOnlyList<BufferedEvent> PeekEvents(int limit)
    {
        using var connection = Open();
        using var command = Command(connection, null, "SELECT event_id, occurred_at, door_id, type, card_number, deny_reason, sequence FROM events ORDER BY occurred_at, event_id LIMIT $limit");
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var events = new List<BufferedEvent>();
        while (reader.Read())
        {
            events.Add(new BufferedEvent(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(2)),
                reader.GetString(3),
                new DateTimeOffset(reader.GetInt64(1), TimeSpan.Zero),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt64(6)));
        }

        return events;
    }

    /// <summary>Deletes events the cloud has acknowledged.</summary>
    public void DeleteEvents(IEnumerable<Guid> eventIds)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var id in eventIds)
        {
            using var delete = Command(connection, transaction, "DELETE FROM events WHERE event_id = $id");
            delete.Parameters.AddWithValue("$id", id.ToString());
            delete.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>Moves events the cloud rejected as invalid out of the buffer so they cannot block it forever.</summary>
    public void RejectEvents(IReadOnlyCollection<BufferedEvent> events, string reason)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var e in events)
        {
            using var insert = Command(connection, transaction, "INSERT OR REPLACE INTO rejected_events (event_id, body, reason) VALUES ($id, $body, $reason)");
            insert.Parameters.AddWithValue("$id", e.EventId.ToString());
            insert.Parameters.AddWithValue("$body", System.Text.Json.JsonSerializer.Serialize(e));
            insert.Parameters.AddWithValue("$reason", reason);
            insert.ExecuteNonQuery();
            using var delete = Command(connection, transaction, "DELETE FROM events WHERE event_id = $id");
            delete.Parameters.AddWithValue("$id", e.EventId.ToString());
            delete.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public long BufferedCount()
    {
        using var connection = Open();
        using var command = Command(connection, null, "SELECT count(*) FROM events");
        return (long)command.ExecuteScalar()!;
    }

    private static void Upsert(SqliteConnection connection, SqliteTransaction transaction, AccessListEntry entry)
    {
        using var upsert = Command(connection, transaction, "INSERT OR REPLACE INTO entries (kind, entity_id, payload) VALUES ($kind, $id, $payload)");
        upsert.Parameters.AddWithValue("$kind", (int)entry.Kind);
        upsert.Parameters.AddWithValue("$id", entry.EntityId);
        upsert.Parameters.AddWithValue("$payload", entry.ToByteArray());
        upsert.ExecuteNonQuery();
    }

    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = Command(connection, transaction, sql);
        command.ExecuteNonQuery();
    }

    private static string? GetMeta(SqliteConnection connection, SqliteTransaction? transaction, string key)
    {
        using var command = Command(connection, transaction, "SELECT value FROM meta WHERE key = $key");
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }

    private static void SetMeta(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = Command(connection, transaction, "INSERT OR REPLACE INTO meta (key, value) VALUES ($key, $value)");
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private string? GetMeta(string key)
    {
        using var connection = Open();
        return GetMeta(connection, null, key);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
