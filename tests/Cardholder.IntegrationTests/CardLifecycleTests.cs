using System.Net;
using System.Net.Http.Json;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.IntegrationTests;

public sealed class CardLifecycleTests(CardholderFixture fixture)
{
    private readonly HttpClient _admin = fixture.CreateClient(AuthConstants.Roles.Admin);

    [Fact]
    public async Task RegisterSite_AsAdmin_ReturnsKeyOnceAndPublishesSiteRegisteredWithHash()
    {
        var (siteId, apiKey) = await _admin.RegisterSiteAsync(timeZone: "America/New_York");

        apiKey.ShouldNotBeNullOrWhiteSpace();
        var published = await fixture.Events.WaitForAsync<SiteRegistered>(e => e.SiteId == siteId);
        published.TimeZoneId.ShouldBe("America/New_York");
        published.Version.ShouldBe(1);
        Convert.FromBase64String(published.ApiKeyHash).ShouldBe(BuildingBlocks.Application.SiteApiKey.TryHash(apiKey));
        published.EventId.ShouldNotBe(Guid.Empty);
        published.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateIssueRevoke_FullFlow_PublishesFullStateEventsInVersionOrder()
    {
        var (siteId, _) = await _admin.RegisterSiteAsync();
        var doorId = await _admin.RegisterDoorAsync(siteId);
        var groupId = await _admin.CreateAccessGroupAsync(doorId);
        var cardholderId = await _admin.CreateCardholderAsync(groupId);
        var number = Api.NewCardNumber();
        var cardId = await _admin.IssueCardAsync(cardholderId, number);

        var revoke = await _admin.PostAsync($"/api/v1/cards/{cardId}/revoke", null, TestContext.Current.CancellationToken);
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await fixture.Events.WaitForAsync<DoorRegistered>(e => e.DoorId == doorId)).SiteId.ShouldBe(siteId);

        var group = await fixture.Events.WaitForAsync<AccessGroupChanged>(e => e.AccessGroupId == groupId);
        group.Doors.ShouldHaveSingleItem().SiteId.ShouldBe(siteId);
        group.Doors[0].Schedule.ShouldHaveSingleItem().ShouldBe(new WeeklyTimeWindow(DayOfWeek.Monday, 480, 1080));

        var cardholder = await fixture.Events.WaitForAsync<CardholderChanged>(e => e.CardholderId == cardholderId);
        cardholder.IsActive.ShouldBeTrue();
        cardholder.AccessGroupIds.ShouldBe([groupId]);

        var issued = await fixture.Events.WaitForAsync<CardIssued>(e => e.CardId == cardId);
        issued.CardNumber.ShouldBe(number);
        issued.CardholderId.ShouldBe(cardholderId);
        issued.IsRevoked.ShouldBeFalse();
        issued.Version.ShouldBe(1);

        var revoked = await fixture.Events.WaitForAsync<CardRevoked>(e => e.CardId == cardId);
        revoked.IsRevoked.ShouldBeTrue();
        revoked.RevokedAt.ShouldNotBeNull();
        revoked.CardNumber.ShouldBe(number);
        revoked.Version.ShouldBe(2);
    }

    [Fact]
    public async Task RevokeCard_Twice_Returns409AndPublishesOnce()
    {
        var (siteId, _) = await _admin.RegisterSiteAsync();
        var groupId = await _admin.CreateAccessGroupAsync(await _admin.RegisterDoorAsync(siteId));
        var cardId = await _admin.IssueCardAsync(await _admin.CreateCardholderAsync(groupId), Api.NewCardNumber());

        (await _admin.PostAsync($"/api/v1/cards/{cardId}/revoke", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var second = await _admin.PostAsync($"/api/v1/cards/{cardId}/revoke", null, TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await fixture.Events.WaitForAsync<CardRevoked>(e => e.CardId == cardId);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        fixture.Events.All<CardRevoked>().Count(e => e.CardId == cardId).ShouldBe(1);
    }

    [Fact]
    public async Task IssueCard_DuplicateNumber_Returns409()
    {
        var cardholderId = await _admin.CreateCardholderAsync();
        var number = Api.NewCardNumber();
        await _admin.IssueCardAsync(cardholderId, number);

        var duplicate = await _admin.PostAsJsonAsync($"/api/v1/cardholders/{cardholderId}/cards", new { cardNumber = number }, Api.Json, TestContext.Current.CancellationToken);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task IssueCard_UnknownCardholder_Returns404()
    {
        var response = await _admin.PostAsJsonAsync($"/api/v1/cardholders/{Guid.NewGuid()}/cards", new { cardNumber = Api.NewCardNumber() }, Api.Json, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Cardholder.NotFound");
    }

    [Fact]
    public async Task RevokeCard_WritesAuditEntryInSameTransaction()
    {
        var cardId = await _admin.IssueCardAsync(await _admin.CreateCardholderAsync(), Api.NewCardNumber());

        await _admin.PostAsync($"/api/v1/cards/{cardId}/revoke", null, TestContext.Current.CancellationToken);

        var entries = await fixture.WithDbAsync(db => db.AuditEntries.AsNoTracking().Where(a => a.EntityId == cardId.ToString()).OrderBy(a => a.OccurredAt).ToListAsync());
        entries.Select(e => e.Action).ShouldBe(["Card.Issued", "Card.Revoked"]);
        entries[1].UserId.ShouldBe("test-admin");
        entries[1].After.ShouldNotBeNull().ShouldContain("isRevoked");
    }
}
