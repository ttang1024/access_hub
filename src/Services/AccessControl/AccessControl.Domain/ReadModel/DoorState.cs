namespace AccessHub.AccessControl.Domain.ReadModel;

public sealed class DoorState : VersionedState
{
    private DoorState()
    {
    }

    public Guid DoorId { get; private set; }

    public Guid SiteId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public static DoorState Create(Guid doorId) => new() { DoorId = doorId };

    public bool Apply(Guid siteId, string name, int version)
    {
        if (!IsNewer(version))
        {
            return false;
        }

        SiteId = siteId;
        Name = name;
        Version = version;
        return true;
    }
}
