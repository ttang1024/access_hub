using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.UnitTests.Domain;

internal sealed class AccessListBuilder
{
    public static readonly Guid Door = Guid.Parse("0195d0f0-0000-7000-8000-00000000d001");
    public static readonly Guid OtherDoor = Guid.Parse("0195d0f0-0000-7000-8000-00000000d002");
    public static readonly Guid Group = Guid.Parse("0195d0f0-0000-7000-8000-00000000a001");
    public static readonly Guid OtherGroup = Guid.Parse("0195d0f0-0000-7000-8000-00000000a002");
    public const string Card = "CARD0001";

    private readonly SiteAccessList _list;

    public AccessListBuilder(string timeZone = "Europe/London")
    {
        _list = new SiteAccessList(timeZone);
        _list.Upsert(new DoorEntry(Door, "Front"));
        _list.Upsert(new DoorEntry(OtherDoor, "Back"));
    }

    public AccessListBuilder WithGroup(Guid groupId, Guid doorId, params ScheduleWindow[] windows)
    {
        var existing = _list.FindGroup(groupId)?.Doors ?? [];
        _list.Upsert(new AccessGroupEntry(groupId, "G", [.. existing, new GroupDoorAccess(doorId, windows)]));
        return this;
    }

    public AccessListBuilder WithCard(bool revoked = false, DateTimeOffset? expiresAt = null, bool active = true, params Guid[] groups)
    {
        _list.Upsert(new CardEntry(Guid.NewGuid(), Card, Guid.NewGuid(), revoked, expiresAt, active, groups.Length == 0 ? [Group] : groups));
        return this;
    }

    public SiteAccessList Build() => _list;

    public static ScheduleWindow AllDay(DayOfWeek day) => new(day, 0, 1440);

    public static ScheduleWindow[] EveryDay() => Enum.GetValues<DayOfWeek>().Select(AllDay).ToArray();
}
