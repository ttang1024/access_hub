using System.Text.Json;
using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>
/// Serialises site entries. The output is deterministic (fixed property order, sorted collections from the
/// calculator), so comparing payload strings tells whether a site's entry actually changed.
/// </summary>
public static class EntryPayloads
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string? Serialize(object? entry) => entry is null ? null : JsonSerializer.Serialize(entry, entry.GetType(), Json);

    public static void ApplyTo(SiteAccessList list, AccessListEntryKind kind, Guid entityId, string? payload)
    {
        if (payload is null)
        {
            list.Remove(kind, entityId);
            return;
        }

        switch (kind)
        {
            case AccessListEntryKind.Door:
                list.Upsert(Deserialize<DoorEntry>(payload));
                break;
            case AccessListEntryKind.AccessGroup:
                list.Upsert(Deserialize<AccessGroupEntry>(payload));
                break;
            case AccessListEntryKind.Card:
                list.Upsert(Deserialize<CardEntry>(payload));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind.");
        }
    }

    public static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, Json) ?? throw new InvalidOperationException($"Empty {typeof(T).Name} payload.");
}
