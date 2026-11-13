namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>An access group as seen by one site: only the doors at that site.</summary>
public sealed record AccessGroupEntry(Guid AccessGroupId, string Name, IReadOnlyList<GroupDoorAccess> Doors)
{
    public GroupDoorAccess? FindDoor(Guid doorId)
    {
        foreach (var door in Doors)
        {
            if (door.DoorId == doorId)
            {
                return door;
            }
        }

        return null;
    }
}
