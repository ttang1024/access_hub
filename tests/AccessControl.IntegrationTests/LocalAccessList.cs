using Grpc.Core;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.IntegrationTests;

/// <summary>A minimal site-side copy of the list, keyed by (kind, entity), to compare edge and cloud.</summary>
internal sealed class LocalAccessList
{
    public Dictionary<(Proto.EntityKind, string), Proto.AccessListEntry> Entries { get; } = [];

    public long Sequence { get; private set; }

    public string TimeZoneId { get; private set; } = string.Empty;

    public static async Task<LocalAccessList> SnapshotAsync(Proto.AccessListSync.AccessListSyncClient client, Metadata credentials)
    {
        var list = new LocalAccessList();
        using var call = client.GetSnapshot(new Proto.GetSnapshotRequest(), credentials);
        await foreach (var chunk in call.ResponseStream.ReadAllAsync())
        {
            list.TimeZoneId = chunk.TimeZoneId;
            foreach (var entry in chunk.Entries)
            {
                list.Entries[(entry.Kind, entry.EntityId)] = entry;
            }

            if (chunk.IsLast)
            {
                list.Sequence = chunk.Sequence;
            }
        }

        return list;
    }

    public void Apply(Proto.AccessListChange change)
    {
        change.Sequence.ShouldBe(Sequence + 1, "changes must arrive in order with no gaps");
        if (change.IsTombstone)
        {
            Entries.Remove((change.Kind, change.EntityId));
        }
        else
        {
            Entries[(change.Kind, change.EntityId)] = change.Entry;
        }

        Sequence = change.Sequence;
    }

    public void ShouldEqual(LocalAccessList other)
    {
        Sequence.ShouldBe(other.Sequence);
        Entries.Keys.OrderBy(k => k.Item2).ShouldBe(other.Entries.Keys.OrderBy(k => k.Item2));
        foreach (var (key, entry) in other.Entries)
        {
            Entries[key].ShouldBe(entry);
        }
    }
}
