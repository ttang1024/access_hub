using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Domain.Doors;

/// <summary>A door with a card reader, belonging to one site.</summary>
public sealed class Door : AggregateRoot<DoorId>
{
    public const int MaxNameLength = 200;

    private Door()
    {
    }

    private Door(DoorId id, SiteId siteId, string name)
        : base(id)
    {
        SiteId = siteId;
        Name = name;
    }

    public SiteId SiteId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public static Result<Door> Register(SiteId siteId, string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return DoorErrors.InvalidName;
        }

        var door = new Door(DoorId.New(), siteId, name.Trim());
        door.Apply(v => new DoorRegisteredDomainEvent(door.Id, siteId, door.Name, v, now));
        return door;
    }
}
