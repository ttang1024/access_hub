using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Features.Rules;
using AccessHub.Alarm.Domain;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using NSubstitute;

namespace AccessHub.Alarm.UnitTests;

public sealed class EvaluateDoorEventsHandlerTests
{
    private static readonly DateTimeOffset T = new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly IDenialStore _store = Substitute.For<IDenialStore>();
    private readonly List<AlarmTrigger> _triggers = [];
    private readonly IAlarmTriggerSink _sink = Substitute.For<IAlarmTriggerSink>();

    public EvaluateDoorEventsHandlerTests() =>
        _sink.TriggerAsync(Arg.Do<AlarmTrigger>(_triggers.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

    [Fact]
    public async Task ThreeDenialsOfOneCardInOneBatch_TriggerOnceOnTheThird()
    {
        StoreHolds();
        var batch = Denials(("C1", 0), ("C1", 20), ("C1", 40), ("C2", 10));

        await Handler().HandleAsync(batch, TestContext.Current.CancellationToken);

        _triggers.ShouldHaveSingleItem().SourceEventId.ShouldBe(batch.Events[2].SourceEventId);
    }

    [Fact]
    public async Task DenialCompletingAWindowWithStoredOnes_Triggers()
    {
        StoreHolds(new StoredDenial(Guid.NewGuid(), "C1", T.AddSeconds(-30)), new StoredDenial(Guid.NewGuid(), "C1", T.AddSeconds(-10)));

        await Handler().HandleAsync(Denials(("C1", 0)), TestContext.Current.CancellationToken);

        _triggers.ShouldHaveSingleItem().Rule.ShouldBe(AlarmRule.RepeatedDenials);
    }

    [Fact]
    public async Task RedeliveredBatch_EventsAlreadyStored_TriggerNothing()
    {
        var batch = Denials(("C1", 0), ("C1", 20), ("C1", 40));
        StoreHolds(batch.Events.Select(e => new StoredDenial(e.SourceEventId, e.CardNumber!, e.OccurredAt)).ToArray());

        await Handler().HandleAsync(batch, TestContext.Current.CancellationToken);

        _triggers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ForcedDoorBatch_TriggersPerEventWithoutTouchingDenials()
    {
        var batch = new DoorEventsRecorded(Guid.NewGuid(), T, Guid.NewGuid(), Guid.NewGuid(), DoorEventType.DoorForced,
            [new RecordedDoorEvent(Guid.NewGuid(), Guid.NewGuid(), T, null, null, null, T), new RecordedDoorEvent(Guid.NewGuid(), Guid.NewGuid(), T, null, null, null, T)]);

        await Handler().HandleAsync(batch, TestContext.Current.CancellationToken);

        _triggers.Count.ShouldBe(2);
        _triggers.ShouldAllBe(t => t.Rule == AlarmRule.DoorForced);
        await _store.DidNotReceiveWithAnyArgs().RecordAsync(default, default!, default, TestContext.Current.CancellationToken);
    }

    private static DoorEventsRecorded Denials(params (string Card, int Seconds)[] denials) =>
        new(Guid.NewGuid(), T, Guid.NewGuid(), Guid.NewGuid(), DoorEventType.AccessDenied,
            denials.Select(d => new RecordedDoorEvent(Guid.NewGuid(), Guid.NewGuid(), T.AddSeconds(d.Seconds), d.Card, "UnknownCard", 1, T)).ToList());

    private void StoreHolds(params StoredDenial[] stored) =>
        _store.RecordAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<StoredDenial>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(stored);

    private EvaluateDoorEventsHandler Handler() => new(_store, _sink);
}
