using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccessHub.Gateway.Tests;

public sealed class SiteRateLimitingTests : IDisposable
{
    // Destinations point at a closed port: requests that pass the limiter fail with 502, limited ones get 429.
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseSetting("RateLimiting:Site:PermitsPerSecond", "1");
        b.UseSetting("RateLimiting:Site:Burst", "3");
        b.UseSetting("ReverseProxy:Clusters:event-ingestion:Destinations:primary:Address", "http://127.0.0.1:9");
        b.UseSetting("ReverseProxy:Clusters:cardholder:Destinations:primary:Address", "http://127.0.0.1:9");
    });

    [Fact]
    public async Task SiteEndpoint_BurstExceeded_Returns429ForThatSiteOnly()
    {
        var noisy = Guid.NewGuid();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            statuses.Add(await PostEventsAsync(noisy));
        }

        statuses.Take(3).ShouldAllBe(s => s != HttpStatusCode.TooManyRequests);
        statuses.Skip(3).ShouldContain(HttpStatusCode.TooManyRequests);
        (await PostEventsAsync(Guid.NewGuid())).ShouldNotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OperatorEndpoint_IsNotSiteRateLimited()
    {
        var client = _factory.CreateClient();
        for (var i = 0; i < 8; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cardholders");
            request.Headers.Add("X-Site-Id", Guid.Empty.ToString());
            (await client.SendAsync(request, TestContext.Current.CancellationToken)).StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        }
    }

    public void Dispose() => _factory.Dispose();

    private async Task<HttpStatusCode> PostEventsAsync(Guid siteId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/sites/{siteId}/events") { Content = new StringContent("{}") };
        request.Headers.Add("X-Site-Id", siteId.ToString());
        return (await _factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken)).StatusCode;
    }
}
