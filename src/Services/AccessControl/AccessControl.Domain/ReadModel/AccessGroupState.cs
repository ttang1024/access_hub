namespace AccessHub.AccessControl.Domain.ReadModel;

public sealed class AccessGroupState : VersionedState
{
    private AccessGroupState()
    {
    }

    public Guid AccessGroupId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public IReadOnlyList<GroupDoorState> Doors { get; private set; } = [];

    public IEnumerable<Guid> SiteIds => Doors.Select(d => d.SiteId).Distinct();

    public static AccessGroupState Create(Guid accessGroupId) => new() { AccessGroupId = accessGroupId };

    public bool Apply(string name, bool isDeleted, IReadOnlyList<GroupDoorState> doors, int version)
    {
        if (!IsNewer(version))
        {
            return false;
        }

        Name = name;
        IsDeleted = isDeleted;
        Doors = isDeleted ? [] : doors;
        Version = version;
        return true;
    }
}
