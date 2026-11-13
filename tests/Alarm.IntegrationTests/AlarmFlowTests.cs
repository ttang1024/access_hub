using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Contracts.IntegrationEvents.Alarm;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Alarm.IntegrationTests;

public sealed class AlarmFlowTests(AlarmFixture fixture)
{
    private static readonly DateTimeOffset T = DateTimeOffset.UtcNow.AddMinutes(-30);

    [Fact]
    public async Task DoorForced_RaisesHighAlarm_PushesToDashboard_AndEscalatesIfNotAcknowledged()
    {
        await using var hub = await fixture.ConnectHubAsync();
        var siteEvents = new ConcurrentBag<JsonElement>();
        var alarmChanges = new ConcurrentBag<JsonElement>();
        hub.On<JsonElement>("siteEvent", siteEvents.Add);
        hub.On<JsonElement>("alarmChanged", alarmChanges.Add);
        var forced = Event(DoorEventType.DoorForced);

        await fixture.Events.PublishStoredEventAsync(forced);

        var raised = await fixture.Events.WaitForAsync<AlarmRaised>(a => a.DoorId == forced.DoorId);
        (raised.Rule, raised.Priority, raised.SiteId).ShouldBe(("DoorForced", "High", forced.SiteId));
        await WaitUntilAsync(() => siteEvents.Any(e => e.GetProperty("eventId").GetGuid() == forced.SourceEventId));
        await WaitUntilAsync(() => alarmChanges.Any(a => a.GetProperty("id").GetGuid() == raised.AlarmId));

        // Real RabbitMQ with the delayed exchange: not acknowledged within the delay, so it escalates.
        await fixture.Events.WaitForAsync<AlarmEscalated>(a => a.AlarmId == raised.AlarmId, AlarmFixture.EscalationDelay * 5);
        var alarm = await GetAlarmAsync(raised.AlarmId);
        alarm.GetProperty("escalated").GetBoolean().ShouldBeTrue();
        alarm.GetProperty("state").GetString().ShouldBe("Raised");
    }

    [Fact]
    public async Task EventsTheRulesIgnore_NeverReachTheRulesQueue_ButStillReachTheLiveFeed()
    {
        await using var hub = await fixture.ConnectHubAsync();
        var pushed = new ConcurrentBag<Guid>();
        hub.On<JsonElement>("siteEvent", e => pushed.Add(e.GetProperty("eventId").GetGuid()));
        var (opened, forced) = (Event(DoorEventType.DoorOpened), Event(DoorEventType.DoorForced));
        var (openedMessage, forcedMessage) = (Guid.NewGuid(), Guid.NewGuid());

        await fixture.Events.PublishStoredEventAsync(opened, openedMessage);
        await fixture.Events.PublishStoredEventAsync(forced, forcedMessage);

        await fixture.Events.WaitForAsync<AlarmRaised>(a => a.DoorId == forced.DoorId);
        await WaitUntilAsync(() => pushed.Contains(opened.SourceEventId) && pushed.Contains(forced.SourceEventId));
        var inboxed = await fixture.WithDbAsync(db => db.Database
            .SqlQuery<Guid>($"SELECT message_id AS \"Value\" FROM alarm.inbox_state WHERE message_id IN ({openedMessage}, {forcedMessage})")
            .ToListAsync());
        inboxed.ShouldBe([forcedMessage], "the topic binding keeps DoorOpened away from the rules consumer, and the live feed has no inbox");
    }

    [Fact]
    public async Task SameDoorForcedTwice_OneOpenAlarmWithTwoOccurrences()
    {
        var door = Guid.NewGuid();
        await fixture.Events.PublishStoredEventAsync(Event(DoorEventType.DoorForced, door: door));
        var raised = await fixture.Events.WaitForAsync<AlarmRaised>(a => a.DoorId == door);

        await fixture.Events.PublishStoredEventAsync(Event(DoorEventType.DoorForced, door: door));

        await WaitUntilAsync(async () => (await GetAlarmAsync(raised.AlarmId)).GetProperty("occurrenceCount").GetInt32() == 2);
        (await fixture.WithDbAsync(db => db.Alarms.CountAsync(a => a.DoorId == door))).ShouldBe(1);
    }

    [Fact]
    public async Task ThreeDenialsWithinAMinute_DeliveredOutOfOrder_RaiseOneRepeatedDenialsAlarm()
    {
        var card = "RD" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        foreach (var offset in new[] { 40, 20, 0 })
        {
            await fixture.Events.PublishStoredEventAsync(Event(DoorEventType.AccessDenied, card, T.AddSeconds(offset)));
        }

        var raised = await fixture.Events.WaitForAsync<AlarmRaised>(a => a.CardNumber == card);
        (raised.Rule, raised.Priority).ShouldBe(("RepeatedDenials", "Medium"));
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        fixture.Events.All<AlarmRaised>().Count(a => a.CardNumber == card).ShouldBe(1);
    }

    [Fact]
    public async Task DenialsSpreadBeyondAMinute_RaiseNothing()
    {
        var card = "SP" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        foreach (var offset in new[] { 0, 45, 90 })
        {
            await fixture.Events.PublishStoredEventAsync(Event(DoorEventType.AccessDenied, card, T.AddSeconds(offset)));
        }

        await WaitUntilAsync(async () => await fixture.WithDbAsync(db => db.RecentDenials.CountAsync(d => d.CardNumber == card)) == 3);
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        fixture.Events.All<AlarmRaised>().ShouldNotContain(a => a.CardNumber == card);
    }

    [Fact]
    public async Task StaleAccessGranted_RaisesHighPriorityStaleGrantAlarm()
    {
        var stale = new StaleAccessGranted(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ST01", Guid.NewGuid(), T, "CardRevoked", 9);

        await fixture.Events.PublishAsync(stale);

        var raised = await fixture.Events.WaitForAsync<AlarmRaised>(a => a.CardNumber == "ST01");
        (raised.Rule, raised.Priority).ShouldBe(("StaleGrant", "High"));
    }

    [Fact]
    public async Task Lifecycle_ThroughTheApi_AcknowledgeThenCloseWithNote_AndInvalidTransitionsAre409()
    {
        var door = Guid.NewGuid();
        await fixture.Events.PublishStoredEventAsync(Event(DoorEventType.DoorHeldOpen, door: door));
        var id = (await fixture.Events.WaitForAsync<AlarmRaised>(a => a.DoorId == door)).AlarmId;
        var operatorClient = fixture.CreateClient(AuthConstants.Roles.Operator);

        (await operatorClient.PostAsJsonAsync($"/api/v1/alarms/{id}/close", new { note = "too early" }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await operatorClient.PostAsync($"/api/v1/alarms/{id}/acknowledge", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await operatorClient.PostAsync($"/api/v1/alarms/{id}/acknowledge", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await operatorClient.PostAsJsonAsync($"/api/v1/alarms/{id}/close", new { note = "" }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await operatorClient.PostAsJsonAsync($"/api/v1/alarms/{id}/close", new { note = "Wedge removed" }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var alarm = await GetAlarmAsync(id);
        alarm.GetProperty("state").GetString().ShouldBe("Closed");
        alarm.GetProperty("acknowledgedBy").GetString().ShouldBe("test-operator");
        alarm.GetProperty("closeNote").GetString().ShouldBe("Wedge removed");
        await fixture.Events.WaitForAsync<AlarmClosed>(a => a.AlarmId == id);
        var audit = await fixture.WithDbAsync(db => db.AuditEntries.AsNoTracking().Where(a => a.EntityId == id.ToString()).Select(a => a.Action).ToListAsync());
        audit.ShouldBe(["Alarm.Acknowledged", "Alarm.Closed"], ignoreOrder: true);
    }

    [Fact]
    public async Task Commands_Authorisation_ViewerCannotAcknowledge_UnknownAlarmIs404()
    {
        (await fixture.CreateClient(AuthConstants.Roles.Viewer).PostAsync($"/api/v1/alarms/{Guid.NewGuid()}/acknowledge", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await fixture.CreateClient(AuthConstants.Roles.Operator).PostAsync($"/api/v1/alarms/{Guid.NewGuid()}/acknowledge", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListOpenAlarms_ExcludesClosed()
    {
        var page = await fixture.CreateClient(AuthConstants.Roles.Viewer).GetFromJsonAsync<JsonElement>("/api/v1/alarms?state=open&pageSize=100", TestContext.Current.CancellationToken);

        page.GetProperty("items").EnumerateArray().ShouldAllBe(a => a.GetProperty("state").GetString() != "Closed");
    }

    private static DoorEventRecorded Event(DoorEventType type, string? card = null, DateTimeOffset? at = null, Guid? door = null) =>
        new(Guid.NewGuid(), at ?? T, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), door ?? Guid.NewGuid(), type, card,
            type == DoorEventType.AccessDenied ? "UnknownCard" : null, 1, DateTimeOffset.UtcNow);

    private static async Task WaitUntilAsync(Func<bool> condition) => await WaitUntilAsync(() => Task.FromResult(condition()));

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!await condition())
        {
            await Task.Delay(100, cts.Token);
        }
    }

    private async Task<JsonElement> GetAlarmAsync(Guid id) =>
        await fixture.CreateClient(AuthConstants.Roles.Viewer).GetFromJsonAsync<JsonElement>($"/api/v1/alarms/{id}");
}
