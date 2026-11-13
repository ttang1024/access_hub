using AccessHub.Alarm.Application.Messages;
using AccessHub.Alarm.Infrastructure.Sagas;
using AccessHub.Contracts.IntegrationEvents.Alarm;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AccessHub.Alarm.UnitTests;

/// <summary>The lifecycle state machine on the MassTransit test harness (in-memory transport and repository).</summary>
public sealed class AlarmSagaTests : IAsyncLifetime
{
    private static readonly TimeSpan EscalationDelay = TimeSpan.FromSeconds(1);
    private readonly IAlarmAudit _audit = Substitute.For<IAlarmAudit>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<AlarmStateMachine, AlarmState> _saga = null!;

    public async ValueTask InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton(_audit)
            .Configure<AlarmOptions>(o => o.EscalationDelay = EscalationDelay)
            .AddMassTransitTestHarness(x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddSagaStateMachine<AlarmStateMachine, AlarmState>().InMemoryRepository();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();
        _saga = _harness.GetSagaStateMachineHarness<AlarmStateMachine, AlarmState>();
    }

    public async ValueTask DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Triggered_NewSubject_RaisesAlarmAndPublishesAlarmRaised()
    {
        var id = await RaiseAsync("DoorForced:door:1", "High");

        (await _harness.Published.Any<AlarmRaised>(m => m.Context.Message.AlarmId == id, TestContext.Current.CancellationToken)).ShouldBeTrue();
        var alarm = _saga.Sagas.Contains(id)!;
        (alarm.OccurrenceCount, alarm.Rule, alarm.Priority).ShouldBe((1, "DoorForced", "High"));
    }

    [Fact]
    public async Task Triggered_SameOpenSubject_IncrementsOccurrencesInsteadOfRaisingAnother()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:2", "Medium");

        await _harness.Bus.Publish(Trigger("DoorHeldOpen:door:2", "Medium"), TestContext.Current.CancellationToken);
        (await _harness.Published.Any<AlarmOccurrenceRecorded>(m => m.Context.Message.OccurrenceCount == 2, TestContext.Current.CancellationToken)).ShouldBeTrue();

        _saga.Sagas.Select(s => s.Saga.SubjectKey == "DoorHeldOpen:door:2", TestContext.Current.CancellationToken).Count().ShouldBe(1);
        _saga.Sagas.Contains(id)!.OccurrenceCount.ShouldBe(2);
    }

    [Fact]
    public async Task Acknowledge_Raised_TransitionsPublishesAuditsAndResponds()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:3", "Medium");

        var response = await AcknowledgeAsync(id);

        response.Is(out Response<AlarmCommandAccepted>? accepted).ShouldBeTrue();
        accepted!.Message.State.ShouldBe("Acknowledged");
        (await _saga.Exists(id, m => m.Acknowledged)).ShouldNotBeNull();
        (await _harness.Published.Any<AlarmAcknowledged>(m => m.Context.Message.AlarmId == id, TestContext.Current.CancellationToken)).ShouldBeTrue();
        _audit.Received(1).Record("Alarm.Acknowledged", id, "op-1", "Raised", "Acknowledged", null);
    }

    [Fact]
    public async Task Close_FromAcknowledgedWithNote_ClosesAndPublishes()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:4", "Medium");
        await AcknowledgeAsync(id);

        var response = await CloseAsync(id, "Door was propped for a delivery");

        response.Is(out Response<AlarmCommandAccepted>? _).ShouldBeTrue();
        (await _saga.Exists(id, m => m.Closed)).ShouldNotBeNull();
        var closed = _harness.Published.Select<AlarmClosed>(TestContext.Current.CancellationToken).Single(m => m.Context.Message.AlarmId == id).Context.Message;
        closed.Note.ShouldBe("Door was propped for a delivery");
    }

    [Fact]
    public async Task Close_FromRaised_IsRejectedAndStateUnchanged()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:5", "Medium");

        var response = await CloseAsync(id, "note");

        Rejection(response).Reason.ShouldBe("InvalidTransition");
        _saga.Sagas.Contains(id)!.CurrentState.ShouldBe("Raised");
        (await _harness.Published.Any<AlarmClosed>(m => m.Context.Message.AlarmId == id, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Close_WithoutNote_IsRejectedAndStaysAcknowledged()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:6", "Medium");
        await AcknowledgeAsync(id);

        var response = await CloseAsync(id, "  ");

        Rejection(response).Reason.ShouldBe("NoteRequired");
        _saga.Sagas.Contains(id)!.CurrentState.ShouldBe("Acknowledged");
    }

    [Fact]
    public async Task Acknowledge_Twice_SecondIsRejected()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:7", "Medium");
        await AcknowledgeAsync(id);

        Rejection(await AcknowledgeAsync(id)).Reason.ShouldBe("InvalidTransition");
    }

    [Fact]
    public async Task Commands_UnknownAlarm_AreRejectedAsNotFound()
    {
        Rejection(await AcknowledgeAsync(Guid.NewGuid())).Reason.ShouldBe("NotFound");
        Rejection(await CloseAsync(Guid.NewGuid(), "x")).Reason.ShouldBe("NotFound");
    }

    [Fact]
    public async Task HighPriority_NotAcknowledgedInTime_EscalatesAndStaysRaised()
    {
        var id = await RaiseAsync("DoorForced:door:8", "High");

        (await _harness.Published.Any<AlarmEscalated>(m => m.Context.Message.AlarmId == id, TestContext.Current.CancellationToken)).ShouldBeTrue();

        var alarm = _saga.Sagas.Contains(id)!;
        alarm.Escalated.ShouldBeTrue();
        alarm.CurrentState.ShouldBe("Raised");
    }

    [Fact]
    public async Task HighPriority_AcknowledgedBeforeDelay_DoesNotEscalate()
    {
        var id = await RaiseAsync("DoorForced:door:9", "High");
        await AcknowledgeAsync(id);

        await Task.Delay(EscalationDelay * 3, TestContext.Current.CancellationToken);

        _harness.Published.Select<AlarmEscalated>(TestContext.Current.CancellationToken).Any(m => m.Context.Message.AlarmId == id).ShouldBeFalse();
        _saga.Sagas.Contains(id)!.Escalated.ShouldBeFalse();
    }

    [Fact]
    public async Task MediumPriority_NeverEscalates()
    {
        var id = await RaiseAsync("DoorHeldOpen:door:10", "Medium");

        await Task.Delay(EscalationDelay * 3, TestContext.Current.CancellationToken);

        _harness.Published.Select<AlarmEscalated>(TestContext.Current.CancellationToken).Any(m => m.Context.Message.AlarmId == id).ShouldBeFalse();
    }

    [Fact]
    public async Task Triggered_AfterSubjectClosed_RaisesANewAlarm()
    {
        var first = await RaiseAsync("DoorHeldOpen:door:11", "Medium");
        await AcknowledgeAsync(first);
        await CloseAsync(first, "done");

        var second = await RaiseAsync("DoorHeldOpen:door:11", "Medium");

        second.ShouldNotBe(first);
        _saga.Sagas.Contains(first)!.CurrentState.ShouldBe("Closed");
    }

    private static AlarmTriggered Trigger(string subject, string priority) =>
        new(subject.Split(':')[0], priority, subject, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow);

    private static AlarmCommandRejected Rejection(Response<AlarmCommandAccepted, AlarmCommandRejected> response)
    {
        response.Is(out Response<AlarmCommandRejected>? rejected).ShouldBeTrue();
        return rejected!.Message;
    }

    private async Task<Guid> RaiseAsync(string subject, string priority)
    {
        await _harness.Bus.Publish(Trigger(subject, priority), TestContext.Current.CancellationToken);
        var ids = await _saga.Exists(a => a.SubjectKey == subject && a.CurrentState != "Closed", m => m.Raised);
        ids.ShouldNotBeNull($"no alarm raised for {subject}");
        return ids.ShouldHaveSingleItem();
    }

    private async Task<Response<AlarmCommandAccepted, AlarmCommandRejected>> AcknowledgeAsync(Guid id) =>
        await _harness.GetRequestClient<AcknowledgeAlarm>().GetResponse<AlarmCommandAccepted, AlarmCommandRejected>(new AcknowledgeAlarm(id, "op-1"), TestContext.Current.CancellationToken);

    private async Task<Response<AlarmCommandAccepted, AlarmCommandRejected>> CloseAsync(Guid id, string note) =>
        await _harness.GetRequestClient<CloseAlarm>().GetResponse<AlarmCommandAccepted, AlarmCommandRejected>(new CloseAlarm(id, "op-1", note), TestContext.Current.CancellationToken);
}
