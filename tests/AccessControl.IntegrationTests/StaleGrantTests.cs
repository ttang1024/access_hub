using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;

namespace AccessHub.AccessControl.IntegrationTests;

public sealed class StaleGrantTests(AccessControlFixture fixture)
{
    [Fact]
    public async Task GrantAfterRevocation_RedeliveredThreeTimes_PublishesExactlyOneStaleAccessGranted()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number, revokedAt));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        var grant = Granted(site, number, revokedAt.AddMinutes(5));

        // Same message redelivered (inbox) and the same event republished under new message IDs (detection key).
        var messageId = Guid.NewGuid();
        await fixture.Events.PublishStoredEventAsync(grant, messageId);
        await fixture.Events.PublishStoredEventAsync(grant, messageId);
        await fixture.Events.PublishStoredEventAsync(grant with { EventId = Guid.NewGuid() });

        var stale = await fixture.Events.WaitForAsync<StaleAccessGranted>(s => s.SourceEventId == grant.SourceEventId);
        (stale.Reason, stale.CardNumber, stale.SiteId, stale.GrantedAt).ShouldBe(("CardRevoked", number, site.SiteId, grant.OccurredAt));
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        fixture.Events.All<StaleAccessGranted>().Count(s => s.SourceEventId == grant.SourceEventId).ShouldBe(1);
    }

    [Fact]
    public async Task GrantBeforeRevocation_IsNotStale()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number, revokedAt));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        var grant = Granted(site, number, revokedAt.AddMinutes(-1));

        await fixture.Events.PublishStoredEventAsync(grant);

        await Task.Delay(2000, TestContext.Current.CancellationToken);
        fixture.Events.All<StaleAccessGranted>().ShouldNotContain(s => s.SourceEventId == grant.SourceEventId);
    }

    [Fact]
    public async Task GrantWhileCardholderInactive_IsStaleWithCardholderInactive()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, _, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        await fixture.Events.PublishAsync(site.Cardholder(holder, active: false));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        var grant = Granted(site, number, DateTimeOffset.UtcNow.AddSeconds(5));

        await fixture.Events.PublishStoredEventAsync(grant);

        (await fixture.Events.WaitForAsync<StaleAccessGranted>(s => s.SourceEventId == grant.SourceEventId)).Reason.ShouldBe("CardholderInactive");
    }

    [Fact]
    public async Task DeniedAndNonDecisionEvents_AreIgnored()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number, DateTimeOffset.UtcNow.AddMinutes(-5)));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        var denied = Granted(site, number, DateTimeOffset.UtcNow) with { Type = DoorEventType.AccessDenied, DenyReason = "CardRevoked" };

        await fixture.Events.PublishStoredEventAsync(denied);

        await Task.Delay(1500, TestContext.Current.CancellationToken);
        fixture.Events.All<StaleAccessGranted>().ShouldNotContain(s => s.SourceEventId == denied.SourceEventId);
    }

    private static DoorEventRecorded Granted(TestSite site, string number, DateTimeOffset at) =>
        new(Guid.NewGuid(), at, Guid.NewGuid(), site.SiteId, Guid.NewGuid(), site.DoorId, DoorEventType.AccessGranted, number, null, 3, DateTimeOffset.UtcNow);
}
