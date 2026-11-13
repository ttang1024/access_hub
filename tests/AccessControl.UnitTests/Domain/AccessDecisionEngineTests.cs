using AccessHub.AccessControl.Domain.AccessLists;
using static AccessHub.AccessControl.UnitTests.Domain.AccessListBuilder;

namespace AccessHub.AccessControl.UnitTests.Domain;

public sealed class AccessDecisionEngineTests
{
    // Wednesday 2026-01-07 12:00 UTC (London is on GMT, so local time is the same).
    private static readonly DateTimeOffset Noon = new(2026, 1, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Decide_ValidCardInSchedule_Grants()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).ShouldBe(AccessDecision.Grant());
    }

    [Fact]
    public void Decide_CardNotInList_DeniesUnknownCard()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.UnknownCard);
    }

    [Fact]
    public void Decide_RevokedCard_DeniesCardRevoked()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(revoked: true).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardRevoked);
    }

    [Fact]
    public void Decide_ExpiredCard_DeniesCardExpired()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(expiresAt: Noon.AddDays(-1)).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardExpired);
    }

    [Fact]
    public void Decide_InactiveCardholder_DeniesCardholderInactive()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(active: false).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardholderInactive);
    }

    [Fact]
    public void Decide_GroupsDoNotIncludeDoor_DeniesNoAccessToDoor()
    {
        var list = new AccessListBuilder().WithGroup(Group, OtherDoor, EveryDay()).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.NoAccessToDoor);
    }

    [Fact]
    public void Decide_CardGroupMissingFromList_DeniesNoAccessToDoor()
    {
        var list = new AccessListBuilder().WithCard(groups: OtherGroup).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.NoAccessToDoor);
    }

    [Fact]
    public void Decide_OutsideWindow_DeniesOutsideSchedule()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 13 * 60, 17 * 60)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.OutsideSchedule);
    }

    [Fact]
    public void Decide_RightTimeWrongDay_DeniesOutsideSchedule()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, AllDay(DayOfWeek.Thursday)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.OutsideSchedule);
    }

    [Fact]
    public void Decide_RevokedAndExpiredAndInactive_ReportsFirstFailingCheck()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(revoked: true, expiresAt: Noon.AddDays(-1), active: false).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardRevoked);
    }

    [Fact]
    public void Decide_ExpiredAndInactive_ReportsCardExpired()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(expiresAt: Noon.AddDays(-1), active: false).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardExpired);
    }

    [Fact]
    public void Decide_InactiveAndNoDoor_ReportsCardholderInactive()
    {
        var list = new AccessListBuilder().WithGroup(Group, OtherDoor, EveryDay()).WithCard(active: false).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardholderInactive);
    }

    [Fact]
    public void Decide_ExpiryExactlyAtTimestamp_DeniesCardExpired()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, EveryDay()).WithCard(expiresAt: Noon).Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Reason.ShouldBe(DenyReason.CardExpired);
        AccessDecisionEngine.Decide(list, Door, Card, Noon.AddTicks(-1)).Granted.ShouldBeTrue();
    }

    [Fact]
    public void Decide_OneGroupOutsideAnotherInside_Grants()
    {
        var list = new AccessListBuilder()
            .WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 0, 60))
            .WithGroup(OtherGroup, Door, new ScheduleWindow(DayOfWeek.Wednesday, 11 * 60, 13 * 60))
            .WithCard(groups: [Group, OtherGroup])
            .Build();

        AccessDecisionEngine.Decide(list, Door, Card, Noon).Granted.ShouldBeTrue();
    }

    [Theory]
    [InlineData(8, 0, 0, true)]     // start is inclusive
    [InlineData(16, 59, 59, true)]
    [InlineData(17, 0, 0, false)]   // end is exclusive
    [InlineData(7, 59, 59, false)]
    public void Decide_WindowBoundaries_StartInclusiveEndExclusive(int hour, int minute, int second, bool granted)
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 8 * 60, 17 * 60)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 7, hour, minute, second, TimeSpan.Zero)).Granted.ShouldBe(granted);
    }

    [Fact]
    public void Decide_WindowToMidnight_CoversLastMinuteButNotNextDay()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 22 * 60, 1440)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 7, 23, 59, 59, TimeSpan.Zero)).Granted.ShouldBeTrue();
        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero)).Reason.ShouldBe(DenyReason.OutsideSchedule);
    }

    [Fact]
    public void Decide_SiteTimeZone_ConvertsTimestampBeforeCheckingSchedule()
    {
        // 09:00-17:00 in New York (UTC-5 in January). 13:30 UTC = 08:30 local: outside. 14:30 UTC = 09:30 local: inside.
        var list = new AccessListBuilder("America/New_York").WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 9 * 60, 17 * 60)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 7, 13, 30, 0, TimeSpan.Zero)).Reason.ShouldBe(DenyReason.OutsideSchedule);
        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 7, 14, 30, 0, TimeSpan.Zero)).Granted.ShouldBeTrue();
    }

    [Fact]
    public void Decide_TimeZoneCrossesDayBoundary_UsesLocalDayOfWeek()
    {
        // Tuesday 20:00 UTC is Wednesday 01:30 in Kolkata (UTC+05:30).
        var list = new AccessListBuilder("Asia/Kolkata").WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 0, 120)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 6, 20, 0, 0, TimeSpan.Zero)).Granted.ShouldBeTrue();
    }

    [Fact]
    public void Decide_SpringForwardDay_UsesSummerOffset()
    {
        // London moves to BST at 01:00 UTC on Sunday 2026-03-29. A Sunday 08:00-17:00 local window:
        // 07:30 UTC is 08:30 BST (inside); 06:30 UTC is 07:30 BST (outside).
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Sunday, 8 * 60, 17 * 60)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 3, 29, 7, 30, 0, TimeSpan.Zero)).Granted.ShouldBeTrue();
        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 3, 29, 6, 30, 0, TimeSpan.Zero)).Reason.ShouldBe(DenyReason.OutsideSchedule);
    }

    [Fact]
    public void Decide_FallBackRepeatedHour_EachInstantJudgedByItsOwnLocalTime()
    {
        // London returns to GMT at 01:00 UTC on Sunday 2026-10-25; local 01:00-02:00 happens twice.
        // Window Sunday 00:00-01:30 local. 00:45 UTC = 01:45 BST (outside); 01:15 UTC = 01:15 GMT (inside).
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Sunday, 0, 90)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 10, 25, 0, 45, 0, TimeSpan.Zero)).Reason.ShouldBe(DenyReason.OutsideSchedule);
        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 10, 25, 1, 15, 0, TimeSpan.Zero)).Granted.ShouldBeTrue();
    }

    [Fact]
    public void Decide_TimestampWithNonUtcOffset_IsTheSameInstant()
    {
        var list = new AccessListBuilder().WithGroup(Group, Door, new ScheduleWindow(DayOfWeek.Wednesday, 11 * 60, 13 * 60)).WithCard().Build();

        AccessDecisionEngine.Decide(list, Door, Card, new DateTimeOffset(2026, 1, 7, 21, 0, 0, TimeSpan.FromHours(9))).Granted.ShouldBeTrue();
    }
}
