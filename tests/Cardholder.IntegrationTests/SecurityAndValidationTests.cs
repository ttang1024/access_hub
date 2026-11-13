using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccessHub.BuildingBlocks.Infrastructure.Auth;

namespace AccessHub.Cardholder.IntegrationTests;

public sealed class SecurityAndValidationTests(CardholderFixture fixture)
{
    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await fixture.CreateClient().GetAsync("/api/v1/sites", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(AuthConstants.Roles.Viewer)]
    [InlineData(AuthConstants.Roles.Operator)]
    public async Task RegisterSite_AsNonAdmin_Returns403(string role)
    {
        var response = await fixture.CreateClient(role).PostAsJsonAsync("/api/v1/sites", new { name = "X", timeZoneId = "UTC" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListSites_AsViewer_Returns200()
    {
        var response = await fixture.CreateClient(AuthConstants.Roles.Viewer).GetAsync("/api/v1/sites", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterSite_InvalidTimeZone_Returns400WithFieldErrors()
    {
        var response = await fixture.CreateClient(AuthConstants.Roles.Admin)
            .PostAsJsonAsync("/api/v1/sites", new { name = "", timeZoneId = "Mars/Olympus" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("Name", out _).ShouldBeTrue();
        errors.TryGetProperty("TimeZoneId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAccessGroup_OverlappingWindowsAndBadMinutes_Returns400()
    {
        var response = await fixture.CreateClient(AuthConstants.Roles.Admin).PostAsJsonAsync("/api/v1/access-groups", new
        {
            name = "Bad",
            doors = new[] { new { doorId = Guid.NewGuid(), windows = new[] { new { day = 1, startMinute = 600, endMinute = 500 } } } },
        }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownRoute_ProblemDetails_DoNotLeakInternals()
    {
        var response = await fixture.CreateClient(AuthConstants.Roles.Admin).GetAsync($"/api/v1/cards/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("traceId");
        body.ShouldNotContain("   at ");
    }
}
