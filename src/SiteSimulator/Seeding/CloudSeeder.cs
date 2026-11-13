using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessHub.SiteSimulator.Seeding;

/// <summary>
/// On first run, registers the simulated sites and their seed data through the Cardholder API (as an operator would),
/// and keeps each site's identity and API key in the data directory. Later runs reuse it.
/// </summary>
public sealed partial class CloudSeeder(HttpClient gateway, TokenClient tokens, IOptions<SimulatorOptions> options, ILogger<CloudSeeder> logger)
{
    private static readonly string[] TimeZones = ["Europe/London", "America/New_York", "Asia/Singapore", "Australia/Sydney"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SiteIdentity>> EnsureSeededAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.Value.DataDirectory, "sites.json");
        if (File.Exists(path))
        {
            return JsonSerializer.Deserialize<List<SiteIdentity>>(await File.ReadAllTextAsync(path, cancellationToken), Json) ?? [];
        }

        var attempt = 0;
        while (true)
        {
            try
            {
                var sites = await SeedAsync(cancellationToken);
                Directory.CreateDirectory(options.Value.DataDirectory);
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sites, Json), cancellationToken);
                return sites;
            }
            catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The gateway or Keycloak may still be starting.
                LogSeedRetry(logger, ++attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(5 * attempt, 30)), cancellationToken);
            }
        }
    }

    private async Task<List<SiteIdentity>> SeedAsync(CancellationToken cancellationToken)
    {
        gateway.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(cancellationToken));
        var run = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..4];
        var o = options.Value;
        var sites = new List<SiteIdentity>();
        for (var s = 0; s < o.Sites; s++)
        {
            var name = $"Site {s + 1}";
            var site = await PostAsync(gateway, "api/v1/sites", new { name, timeZoneId = TimeZones[s % TimeZones.Length] }, cancellationToken);
            var siteId = site.GetProperty("siteId").GetGuid();
            var apiKey = site.GetProperty("apiKey").GetString()!;

            var doors = new List<Guid>();
            for (var d = 0; d < o.DoorsPerSite; d++)
            {
                doors.Add((await PostAsync(gateway, $"api/v1/sites/{siteId}/doors", new { name = $"{name} door {d + 1}" }, cancellationToken)).GetProperty("id").GetGuid());
            }

            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            var staff = await CreateGroupAsync($"{name} staff", doors, weekdays.Select(day => new { day = day.ToString(), startMinute = 6 * 60, endMinute = 22 * 60 }), cancellationToken);
            var security = await CreateGroupAsync($"{name} security", doors, Enum.GetValues<DayOfWeek>().Select(day => new { day = day.ToString(), startMinute = 0, endMinute = 1440 }), cancellationToken);

            var cards = new List<string>();
            for (var c = 0; c < o.CardholdersPerSite; c++)
            {
                var group = c % 10 == 0 ? security : staff;
                var holder = (await PostAsync(gateway, "api/v1/cardholders", new { firstName = $"Person{c + 1}", lastName = $"{name.Replace(" ", string.Empty, StringComparison.Ordinal)}", accessGroupIds = new[] { group } }, cancellationToken)).GetProperty("id").GetGuid();
                var number = $"{run}{s:D2}{c:D4}";
                await PostAsync(gateway, $"api/v1/cardholders/{holder}/cards", new { cardNumber = number }, cancellationToken);
                cards.Add(number);
            }

            sites.Add(new SiteIdentity(siteId, name, apiKey, doors, cards));
            LogSeeded(logger, name, siteId, doors.Count, cards.Count);
        }

        return sites;
    }

    private async Task<Guid> CreateGroupAsync<TWindow>(string name, IEnumerable<Guid> doors, IEnumerable<TWindow> windows, CancellationToken cancellationToken)
    {
        var windowList = windows.ToList();
        var body = new { name, doors = doors.Select(doorId => new { doorId, windows = windowList }).ToList() };
        return (await PostAsync(gateway, "api/v1/access-groups", body, cancellationToken)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> PostAsync(HttpClient client, string path, object body, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(path, body, Json, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"POST {path} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Site} ({SiteId}) with {Doors} doors and {Cards} cards")]
    private static partial void LogSeeded(ILogger logger, string site, Guid siteId, int doors, int cards);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Seeding attempt {Attempt} failed ({Reason}); retrying")]
    private static partial void LogSeedRetry(ILogger logger, int attempt, string reason);
}
