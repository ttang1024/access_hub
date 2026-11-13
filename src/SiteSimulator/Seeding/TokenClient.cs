using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AccessHub.SiteSimulator.Seeding;

/// <summary>Gets an admin token for seeding with the OAuth client-credentials grant.</summary>
public sealed class TokenClient(HttpClient http, IOptions<SimulatorOptions> options)
{
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var o = options.Value;
        using var response = await http.PostAsync(
            o.TokenEndpoint ?? throw new InvalidOperationException("Simulator:TokenEndpoint is not configured."),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = o.ClientId,
                ["client_secret"] = o.ClientSecret ?? throw new InvalidOperationException("Simulator:ClientSecret is not configured."),
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        return token?.AccessToken ?? throw new InvalidOperationException("The token endpoint returned no access token.");
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
