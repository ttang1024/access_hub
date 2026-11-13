using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.IntegrationTests;

public sealed class CardholderUpdateTests(CardholderFixture fixture)
{
    private readonly HttpClient _admin = fixture.CreateClient(AuthConstants.Roles.Admin);

    [Fact]
    public async Task UpdateCardholder_Deactivate_PublishesNewVersionWithFullState()
    {
        var (siteId, _) = await _admin.RegisterSiteAsync();
        var groupId = await _admin.CreateAccessGroupAsync(await _admin.RegisterDoorAsync(siteId));
        var id = await _admin.CreateCardholderAsync(groupId);

        var response = await _admin.PutAsJsonAsync($"/api/v1/cardholders/{id}", new { firstName = "Grace", lastName = "Hopper", isActive = false, accessGroupIds = new[] { groupId }, expectedVersion = 1 }, Api.Json, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var changed = await fixture.Events.WaitForAsync<CardholderChanged>(e => e.CardholderId == id && e.Version == 2);
        changed.IsActive.ShouldBeFalse();
        changed.AccessGroupIds.ShouldBe([groupId]);
    }

    [Fact]
    public async Task UpdateCardholder_StaleExpectedVersion_Returns409()
    {
        var id = await _admin.CreateCardholderAsync();
        var body = new { firstName = "A", lastName = "B", isActive = true, accessGroupIds = Array.Empty<Guid>(), expectedVersion = 1 };
        (await _admin.PutAsJsonAsync($"/api/v1/cardholders/{id}", body, Api.Json, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var stale = await _admin.PutAsJsonAsync($"/api/v1/cardholders/{id}", body with { firstName = "C" }, Api.Json, TestContext.Current.CancellationToken);

        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCardholder_UnknownAccessGroup_Returns404()
    {
        var response = await _admin.PostAsJsonAsync("/api/v1/cardholders", new { firstName = "A", lastName = "B", accessGroupIds = new[] { Guid.NewGuid() } }, Api.Json, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListCardholders_Paged_ReturnsTotalCountAndRespectsPageSize()
    {
        var unique = "Pg" + Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            (await _admin.PostAsJsonAsync("/api/v1/cardholders", new { firstName = "P", lastName = unique + i }, Api.Json, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        }

        var page = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/cardholders?search={unique}&page=1&pageSize=2", Api.Json, TestContext.Current.CancellationToken);

        page.GetProperty("totalCount").GetInt32().ShouldBe(3);
        page.GetProperty("items").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task ListCardholders_PageSizeAbove100_Returns400()
    {
        var response = await _admin.GetAsync("/api/v1/cardholders?pageSize=101", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
