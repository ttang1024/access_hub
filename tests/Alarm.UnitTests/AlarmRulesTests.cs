using AccessHub.Alarm.Domain;

namespace AccessHub.Alarm.UnitTests;

public sealed class AlarmRulesTests
{
    private static readonly DateTimeOffset T = new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("DoorForced", AlarmRule.DoorForced, AlarmPriority.High)]
    [InlineData("DoorHeldOpen", AlarmRule.DoorHeldOpen, AlarmPriority.Medium)]
    public void ForDoorEvent_DoorAlarmTypes_RaiseOnTheDoorSubject(string type, AlarmRule rule, AlarmPriority priority)
    {
        var door = Guid.NewGuid();

        var trigger = AlarmRules.ForDoorEvent(type, Guid.NewGuid(), door, Guid.NewGuid(), T).ShouldNotBeNull();

        (trigger.Rule, trigger.Priority, trigger.SubjectKey).ShouldBe((rule, priority, $"{rule}:door:{door}"));
    }

    [Theory]
    [InlineData("AccessGranted")]
    [InlineData("DoorOpened")]
    [InlineData("AccessDenied")]
    public void ForDoorEvent_OtherTypes_RaiseNothing(string type) =>
        AlarmRules.ForDoorEvent(type, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T).ShouldBeNull();

    [Fact]
    public void StaleGrant_IsHighPriorityOnTheCard() =>
        AlarmRules.StaleGrant(Guid.NewGuid(), Guid.NewGuid(), "C1", Guid.NewGuid(), T).ShouldSatisfyAllConditions(
            t => t.Priority.ShouldBe(AlarmPriority.High),
            t => t.SubjectKey.ShouldBe("StaleGrant:card:C1"));

    [Fact]
    public void RepeatedDenials_ThreeWithinSixtySeconds_Triggers() =>
        AlarmRules.RepeatedDenialsTriggered([T, T.AddSeconds(30), T.AddSeconds(60)], T.AddSeconds(60)).ShouldBeTrue();

    [Fact]
    public void RepeatedDenials_ThreeSpreadOverMoreThanSixtySeconds_DoesNotTrigger() =>
        AlarmRules.RepeatedDenialsTriggered([T, T.AddSeconds(30), T.AddSeconds(61)], T.AddSeconds(61)).ShouldBeFalse();

    [Fact]
    public void RepeatedDenials_OnlyTwo_DoesNotTrigger() =>
        AlarmRules.RepeatedDenialsTriggered([T, T.AddSeconds(1)], T.AddSeconds(1)).ShouldBeFalse();

    [Fact]
    public void RepeatedDenials_OutOfOrderArrival_EarliestArrivingLastStillTriggers() =>
        // The denial at T arrives after T+20 and T+40 were already stored: still a window of three.
        AlarmRules.RepeatedDenialsTriggered([T.AddSeconds(20), T.AddSeconds(40), T], T).ShouldBeTrue();

    [Fact]
    public void RepeatedDenials_NewDenialOutsideTheDenseWindow_DoesNotTrigger() =>
        // Three denials cluster at T..T+10, but the new one at T+90 is not in any window of three.
        AlarmRules.RepeatedDenialsTriggered([T, T.AddSeconds(5), T.AddSeconds(10), T.AddSeconds(90)], T.AddSeconds(90)).ShouldBeFalse();

    [Fact]
    public void Note_RequiredAndBounded()
    {
        AlarmErrors.IsValidNote(" ").ShouldBeFalse();
        AlarmErrors.IsValidNote(new string('x', AlarmErrors.MaxNoteLength + 1)).ShouldBeFalse();
        AlarmErrors.IsValidNote("Checked on site").ShouldBeTrue();
    }
}
