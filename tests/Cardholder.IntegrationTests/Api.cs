using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccessHub.Cardholder.IntegrationTests;

/// <summary>Small typed helpers over the HTTP API so tests read as scenarios.</summary>
internal static class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task<(Guid SiteId, string ApiKey)> RegisterSiteAsync(this HttpClient client, string name = "HQ", string timeZone = "Europe/London")
    {
        var response = await client.PostAsJsonAsync("/api/v1/sites", new { name, timeZoneId = timeZone }, Json);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = (await response.Content.ReadFromJsonAsync<JsonElement>(Json))!;
        return (body.GetProperty("siteId").GetGuid(), body.GetProperty("apiKey").GetString()!);
    }

    public static async Task<Guid> RegisterDoorAsync(this HttpClient client, Guid siteId, string name = "Front door") =>
        await CreatedIdAsync(await client.PostAsJsonAsync($"/api/v1/sites/{siteId}/doors", new { name }, Json));

    public static async Task<Guid> CreateAccessGroupAsync(this HttpClient client, Guid doorId, string name = "Staff") =>
        await CreatedIdAsync(await client.PostAsJsonAsync("/api/v1/access-groups", new
        {
            name,
            doors = new[] { new { doorId, windows = new[] { new { day = "Monday", startMinute = 480, endMinute = 1080 } } } },
        }, Json));

    public static async Task<Guid> CreateCardholderAsync(this HttpClient client, params Guid[] groupIds) =>
        await CreatedIdAsync(await client.PostAsJsonAsync("/api/v1/cardholders", new { firstName = "Grace", lastName = "Hopper", accessGroupIds = groupIds }, Json));

    public static async Task<Guid> IssueCardAsync(this HttpClient client, Guid cardholderId, string number) =>
        await CreatedIdAsync(await client.PostAsJsonAsync($"/api/v1/cardholders/{cardholderId}/cards", new { cardNumber = number }, Json));

    public static string NewCardNumber() => "C" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    private static async Task<Guid> CreatedIdAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
    }
}
