using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.UnitTests.Domain;

public sealed class AccessGroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-1, 60)]
    [InlineData(60, 60)]
    [InlineData(120, 60)]
    [InlineData(0, 1441)]
    public void TimeWindow_InvalidRange_ReturnsValidationError(int start, int end) =>
        TimeWindow.Create(DayOfWeek.Monday, start, end).Error!.Type.ShouldBe(ErrorType.Validation);

    [Fact]
    public void TimeWindow_FullDay_IsValid() =>
        TimeWindow.Create(DayOfWeek.Sunday, 0, TimeWindow.MinutesPerDay).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Schedule_OverlappingWindowsSameDay_ReturnsValidationError()
    {
        var result = Schedule.Create([Window(DayOfWeek.Monday, 480, 720), Window(DayOfWeek.Monday, 700, 900)]);

        result.Error!.Code.ShouldBe("AccessGroup.InvalidSchedule");
    }

    [Fact]
    public void Schedule_AdjacentWindows_AreAllowedAndSorted()
    {
        var schedule = Schedule.Create([Window(DayOfWeek.Tuesday, 0, 60), Window(DayOfWeek.Monday, 720, 900), Window(DayOfWeek.Monday, 480, 720)]).Value;

        schedule.Windows.Select(w => (w.Day, w.StartMinute)).ShouldBe([(DayOfWeek.Monday, 480), (DayOfWeek.Monday, 720), (DayOfWeek.Tuesday, 0)]);
    }

    [Fact]
    public void Create_DuplicateDoor_ReturnsValidationError()
    {
        var door = Access(DoorId.New());

        AccessGroup.Create("Staff", [door, door], Now).Error.ShouldBe(AccessGroupErrors.DuplicateDoor);
    }

    [Fact]
    public void Update_SameContent_RaisesNothing()
    {
        var doors = new[] { Access(DoorId.New()) };
        var group = AccessGroup.Create("Staff", doors, Now).Value;
        group.ClearDomainEvents();

        group.Update("Staff", [Access(doors[0].DoorId, doors[0].SiteId)], null, Now).IsSuccess.ShouldBeTrue();

        group.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Delete_Group_RaisesTombstoneWithNoDoors()
    {
        var group = AccessGroup.Create("Staff", [Access(DoorId.New())], Now).Value;
        group.ClearDomainEvents();

        group.Delete(1, Now);

        var changed = group.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AccessGroupChangedDomainEvent>();
        changed.IsDeleted.ShouldBeTrue();
        changed.Doors.ShouldBeEmpty();
        group.Update("Staff", [], null, Now).Error!.Type.ShouldBe(ErrorType.Conflict);
    }

    private static TimeWindow Window(DayOfWeek day, int start, int end) => TimeWindow.Create(day, start, end).Value;

    private static DoorAccess Access(DoorId door, SiteId? site = null) =>
        new(door, site ?? new SiteId(Guid.Parse("0195d0f0-0000-7000-8000-000000000001")), Schedule.Create([Window(DayOfWeek.Monday, 480, 1080)]).Value);
}
