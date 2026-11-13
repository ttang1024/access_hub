using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Projection;
using AccessHub.AccessControl.Domain.AccessLists;
using Google.Protobuf.WellKnownTypes;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.Api.Grpc;

/// <summary>Explicit mapping from stored entry payloads to the wire format.</summary>
internal static class ProtoMapper
{
    public static Proto.AccessListEntry ToProto(AccessListEntryKind kind, Guid entityId, string payload)
    {
        var entry = new Proto.AccessListEntry { Kind = ToProto(kind), EntityId = entityId.ToString() };
        switch (kind)
        {
            case AccessListEntryKind.Door:
                var door = EntryPayloads.Deserialize<DoorEntry>(payload);
                entry.Door = new Proto.DoorEntry { Name = door.Name };
                break;
            case AccessListEntryKind.AccessGroup:
                var group = EntryPayloads.Deserialize<AccessGroupEntry>(payload);
                entry.AccessGroup = new Proto.AccessGroupEntry { Name = group.Name };
                entry.AccessGroup.Doors.AddRange(group.Doors.Select(d =>
                {
                    var access = new Proto.GroupDoorAccess { DoorId = d.DoorId.ToString() };
                    access.Windows.AddRange(d.Windows.Select(w => new Proto.TimeWindow { DayOfWeek = (int)w.Day, StartMinute = w.StartMinute, EndMinute = w.EndMinute }));
                    return access;
                }));
                break;
            case AccessListEntryKind.Card:
                var card = EntryPayloads.Deserialize<CardEntry>(payload);
                entry.Card = new Proto.CardEntry
                {
                    CardNumber = card.CardNumber,
                    CardholderId = card.CardholderId.ToString(),
                    IsRevoked = card.IsRevoked,
                    ExpiresAt = card.ExpiresAt is { } expires ? Timestamp.FromDateTimeOffset(expires) : null,
                    CardholderActive = card.CardholderActive,
                };
                entry.Card.AccessGroupIds.AddRange(card.AccessGroupIds.Select(g => g.ToString()));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind.");
        }

        return entry;
    }

    public static Proto.AccessListChange ToProto(SiteChangeRecord change)
    {
        var message = new Proto.AccessListChange
        {
            Sequence = change.Sequence,
            Kind = ToProto(change.Kind),
            EntityId = change.EntityId.ToString(),
            IsTombstone = change.Payload is null,
            ChangedAt = Timestamp.FromDateTimeOffset(change.ChangedAt),
        };
        if (change.Payload is not null)
        {
            message.Entry = ToProto(change.Kind, change.EntityId, change.Payload);
        }

        return message;
    }

    private static Proto.EntityKind ToProto(AccessListEntryKind kind) => kind switch
    {
        AccessListEntryKind.Door => Proto.EntityKind.Door,
        AccessListEntryKind.AccessGroup => Proto.EntityKind.AccessGroup,
        AccessListEntryKind.Card => Proto.EntityKind.Card,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind."),
    };
}
