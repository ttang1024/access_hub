using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Domain.ReadModel;

/// <summary>
/// Derives what one site's access list should contain for one entity, from the read model. Pure: the projector
/// compares the result with what the site currently holds and appends a change only when they differ.
/// </summary>
public static class SiteEntryCalculator
{
    public static DoorEntry? ForDoor(DoorState? door, Guid siteId) =>
        door is not null && door.Version > 0 && door.SiteId == siteId ? new DoorEntry(door.DoorId, door.Name) : null;

    public static AccessGroupEntry? ForGroup(AccessGroupState? group, Guid siteId)
    {
        if (group is null || group.IsDeleted)
        {
            return null;
        }

        var doors = group.Doors
            .Where(d => d.SiteId == siteId)
            .OrderBy(d => d.DoorId)
            .Select(d => new GroupDoorAccess(d.DoorId, d.Windows.OrderBy(w => w.Day).ThenBy(w => w.StartMinute).ToList()))
            .ToList();
        return doors.Count == 0 ? null : new AccessGroupEntry(group.AccessGroupId, group.Name, doors);
    }

    /// <summary>
    /// A card belongs to a site's list when its cardholder is in at least one group that includes a door at the site.
    /// Only those groups are listed on the card.
    /// </summary>
    public static CardEntry? ForCard(CardState? card, CardholderState? cardholder, IReadOnlyDictionary<Guid, AccessGroupState> groups, Guid siteId)
    {
        if (card is null || cardholder is null || card.CardholderId != cardholder.CardholderId)
        {
            return null;
        }

        var siteGroups = cardholder.AccessGroupIds
            .Where(id => groups.TryGetValue(id, out var g) && !g.IsDeleted && g.Doors.Any(d => d.SiteId == siteId))
            .Order()
            .ToList();
        return siteGroups.Count == 0
            ? null
            : new CardEntry(card.CardId, card.Number, card.CardholderId, card.IsRevoked, card.ExpiresAt, cardholder.IsActive, siteGroups);
    }
}
