using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccessHub.BuildingBlocks.Infrastructure.Auth;

namespace AccessHub.AccessControl.IntegrationTests;

public sealed class OperatorEndpointTests(AccessControlFixture fixture)
{
    [Fact]
    public async Task Explain_RunsEngineAgainstCloudList()
    {
        var site = await new TestSite(fixture).RegisterAsync();
        var (holder, card, number) = await site.AddCardAsync();
        await fixture.WaitForSequenceAsync(site.SiteId, 3);
        var client = fixture.CreateClient(AuthConstants.Roles.Viewer);

        var granted = await ExplainAsync(client, site, number);
        await fixture.Events.PublishAsync(site.Revoke(card, holder, number));
        await fixture.WaitForSequenceAsync(site.SiteId, 4);
        var denied = await ExplainAsync(client, site, number);
        var unknown = await ExplainAsync(client, site, "NOPE1234");

        granted.GetProperty("granted").GetBoolean().ShouldBeTrue();
        denied.GetProperty("denyReason").GetString().ShouldBe("CardRevoked");
        denied.GetProperty("accessListSequence").GetInt64().ShouldBe(4);
        unknown.GetProperty("denyReason").GetString().ShouldBe("UnknownCard");
    }

    [Fact]
    public async Task Explain_WithoutToken_Returns401()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/v1/access/explain", new { siteId = Guid.NewGuid(), doorId = Guid.NewGuid(), cardNumber = "X" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncStatus_ListsRegisteredSitesWithLag()
    {
        var site = await new TestSite(fixture).RegisterAsync();

        var statuses = await fixture.CreateClient(AuthConstants.Roles.Operator).GetFromJsonAsync<JsonElement>("/api/v1/sites/sync-status", TestContext.Current.CancellationToken);

        var mine = statuses.EnumerateArray().Single(s => s.GetProperty("siteId").GetGuid() == site.SiteId);
        mine.GetProperty("latestSequence").GetInt64().ShouldBe(2);
        mine.GetProperty("lag").GetInt64().ShouldBe(2);
        mine.GetProperty("isConnected").GetBoolean().ShouldBeFalse();
    }

    private static async Task<JsonElement> ExplainAsync(HttpClient client, TestSite site, string number)
    {
        var response = await client.PostAsJsonAsync("/api/v1/access/explain", new { siteId = site.SiteId, doorId = site.DoorId, cardNumber = number });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
