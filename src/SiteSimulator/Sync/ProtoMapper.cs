using AccessHub.AccessControl.Domain.AccessLists;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.SiteSimulator.Sync;

/// <summary>Explicit mapping from the wire format to the shared domain model the engine decides on.</summary>
internal static class ProtoMapper
{
    public static SiteAccessList ToAccessList(string timeZoneId, IEnumerable<Proto.AccessListEntry> entries)
    {
        var list = new SiteAccessList(timeZoneId);
        foreach (var entry in entries)
        {
            Apply(list, entry);
        }

        return list;
    }

    public static void Apply(SiteAccessList list, Proto.AccessListChange change)
    {
        if (change.IsTombstone)
        {
            list.Remove(ToKind(change.Kind), Guid.Parse(change.EntityId));
        }
        else
        {
            Apply(list, change.Entry);
        }
    }

    private static void Apply(SiteAccessList list, Proto.AccessListEntry entry)
    {
        var id = Guid.Parse(entry.EntityId);
        switch (entry.PayloadCase)
        {
            case Proto.AccessListEntry.PayloadOneofCase.Door:
                list.Upsert(new DoorEntry(id, entry.Door.Name));
                break;
            case Proto.AccessListEntry.PayloadOneofCase.AccessGroup:
                list.Upsert(new AccessGroupEntry(id, entry.AccessGroup.Name, entry.AccessGroup.Doors
                    .Select(d => new GroupDoorAccess(Guid.Parse(d.DoorId), d.Windows.Select(w => new ScheduleWindow((DayOfWeek)w.DayOfWeek, w.StartMinute, w.EndMinute)).ToList()))
                    .ToList()));
                break;
            case Proto.AccessListEntry.PayloadOneofCase.Card:
                var card = entry.Card;
                list.Upsert(new CardEntry(
                    id, card.CardNumber, Guid.Parse(card.CardholderId), card.IsRevoked, card.ExpiresAt?.ToDateTimeOffset(), card.CardholderActive,
                    card.AccessGroupIds.Select(Guid.Parse).ToList()));
                break;
            default:
                throw new InvalidOperationException($"Entry {entry.EntityId} has no payload.");
        }
    }

    private static AccessListEntryKind ToKind(Proto.EntityKind kind) => kind switch
    {
        Proto.EntityKind.Door => AccessListEntryKind.Door,
        Proto.EntityKind.AccessGroup => AccessListEntryKind.AccessGroup,
        Proto.EntityKind.Card => AccessListEntryKind.Card,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entity kind."),
    };
}
