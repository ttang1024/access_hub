using AccessHub.AccessControl.Domain.AccessLists;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.AccessControl.IntegrationTests;

public sealed class ProjectionTests(AccessControlFixture fixture)
{
    [Fact]
    public async Task CardholderDeactivated_UpdatesCardEntryAtEverySiteOfItsGroups()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, _) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);

        await fixture.Events.PublishAsync(site.Cardholder(holder, active: false));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);

        var change = await fixture.WithDbAsync(db => db.SiteChanges.AsNoTracking().SingleAsync(c => c.SiteId == site.SiteId && c.Sequence == 4));
        change.EntityId.ShouldBe(card);
        change.Payload.ShouldNotBeNull().ShouldContain("\"cardholderActive\":false");
    }

    [Fact]
    public async Task CardholderLeavesGroup_CardTombstonedFromSite()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, _) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);

        await fixture.Events.PublishAsync(site.Cardholder(holder, groups: Guid.NewGuid()));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);

        var change = await fixture.WithDbAsync(db => db.SiteChanges.AsNoTracking().SingleAsync(c => c.SiteId == site.SiteId && c.Sequence == 4));
        (change.Kind, change.EntityId, change.Payload).ShouldBe((AccessListEntryKind.Card, card, (string?)null));
        (await fixture.WithDbAsync(db => db.SiteEntries.AnyAsync(e => e.SiteId == site.SiteId && e.EntityId == card))).ShouldBeFalse();
    }

    [Fact]
    public async Task CardBeforeCardholderBeforeGroup_OutOfOrderArrival_ConvergesToSameList()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = (Guid.CreateVersion7(), Guid.CreateVersion7(), TestSite.NewCardNumber());
        var group = Guid.CreateVersion7();

        // Card first (cardholder unknown), then cardholder in a group not yet known, then the group: only the last
        // event makes the card belong to the site.
        await fixture.Events.PublishAsync(site.Card(card, holder, number));
        await fixture.Events.PublishAsync(site.Cardholder(holder, groups: group));
        await Task.Delay(500, TestContext.Current.CancellationToken);
        (await fixture.WithDbAsync(db => db.SiteEntries.AnyAsync(e => e.EntityId == card))).ShouldBeFalse();

        await fixture.Events.PublishAsync(new Contracts.IntegrationEvents.Cardholder.AccessGroupChanged(
            Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), group, "Late", false, [new(site.DoorId, site.SiteId, TestSite.AllWeek)], 1));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);

        var entries = await fixture.WithDbAsync(db => db.SiteEntries.AsNoTracking().Where(e => e.SiteId == site.SiteId).Select(e => new { e.Kind, e.EntityId }).ToListAsync());
        entries.ShouldContain(new { Kind = AccessListEntryKind.Card, EntityId = card });
        entries.ShouldContain(new { Kind = AccessListEntryKind.AccessGroup, EntityId = group });
    }

    [Fact]
    public async Task AccessGroupDeleted_RemovesGroupAndMembersCardsFromSite()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (_, card, _) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);

        await fixture.Events.PublishAsync(new Contracts.IntegrationEvents.Cardholder.AccessGroupChanged(
            Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), site.GroupId, "Staff", true, [], 99));
        await fixture.WaitForSequenceAsync(site.SiteId, 5);

        var remaining = await fixture.WithDbAsync(db => db.SiteEntries.AsNoTracking().Where(e => e.SiteId == site.SiteId).Select(e => e.Kind).ToListAsync());
        remaining.ShouldBe([AccessListEntryKind.Door]);
        (await fixture.WithDbAsync(db => db.SiteEntries.AnyAsync(e => e.EntityId == card))).ShouldBeFalse();
    }
}
