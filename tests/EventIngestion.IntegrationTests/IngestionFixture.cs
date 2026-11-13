using System.Net.Http.Headers;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using AccessHub.EventIngestion.Infrastructure;
using AccessHub.EventIngestion.Infrastructure.Persistence;
using AccessHub.Testing;
using Microsoft.EntityFrameworkCore;

[assembly: AssemblyFixture(typeof(AccessHub.EventIngestion.IntegrationTests.IngestionFixture))]

namespace AccessHub.EventIngestion.IntegrationTests;

public sealed class IngestionFixture : ServiceFixture<Program, IngestionDbContext>
{
    protected override string DatabaseConnectionName => EventIngestionInfrastructureRegistration.ConnectionName;

    protected override IEnumerable<Type> ListenedEventTypes => [typeof(DoorEventsRecorded)];

    /// <summary>Registers a site through the real broker and returns a client authenticated as that site.</summary>
    public async Task<(Guid SiteId, string ApiKey, HttpClient Client)> RegisterSiteAsync(int version = 1, Guid? siteId = null)
    {
        var id = siteId ?? Guid.CreateVersion7();
        var (key, hash) = SiteApiKey.Generate();
        await Events.PublishAsync(new SiteRegistered(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), id, "S", "UTC", Convert.ToBase64String(hash), version));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (await WithDbAsync(db => db.Sites.AsNoTracking().Where(s => s.SiteId == id).Select(s => s.Version).FirstOrDefaultAsync()) < version)
        {
            await Task.Delay(50, cts.Token);
        }

        return (id, key, SiteClient(id, key));
    }

    public HttpClient SiteClient(Guid siteId, string apiKey)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthConstants.Headers.SiteId, siteId.ToString());
        client.DefaultRequestHeaders.Add(AuthConstants.Headers.ApiKey, apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
