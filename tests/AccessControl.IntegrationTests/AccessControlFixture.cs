using AccessHub.AccessControl.Infrastructure;
using AccessHub.AccessControl.Infrastructure.Persistence;
using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Testing;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

[assembly: AssemblyFixture(typeof(AccessHub.AccessControl.IntegrationTests.AccessControlFixture))]

namespace AccessHub.AccessControl.IntegrationTests;

public sealed class AccessControlFixture : ServiceFixture<Program, AccessControlDbContext>
{
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(1);

    protected override string DatabaseConnectionName => AccessControlInfrastructureRegistration.ConnectionName;

    protected override IEnumerable<Type> ListenedEventTypes => [typeof(StaleAccessGranted)];

    public Proto.AccessListSync.AccessListSyncClient SyncClient()
    {
        var channel = GrpcChannel.ForAddress(Factory.Server.BaseAddress, new GrpcChannelOptions { HttpHandler = Factory.Server.CreateHandler() });
        return new Proto.AccessListSync.AccessListSyncClient(channel);
    }

    /// <summary>Waits until the projection has appended changes up to <paramref name="sequence"/> for the site.</summary>
    public async Task WaitForSequenceAsync(Guid siteId, long sequence, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(20));
        long current = -1;
        while (!cts.IsCancellationRequested)
        {
            current = await WithDbAsync(db => db.Sites.AsNoTracking().Where(s => s.SiteId == siteId).Select(s => s.LastSequence).FirstOrDefaultAsync());
            if (current >= sequence)
            {
                return;
            }

            await Task.Delay(50, CancellationToken.None);
        }

        throw new TimeoutException($"Site {siteId} reached sequence {current}, expected {sequence}.");
    }

    /// <summary>Waits until a message has been fully processed (consumers are idle), by publishing a marker and waiting for it.</summary>
    public async Task WaitForSiteVersionAsync(Guid siteId, int version)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!cts.IsCancellationRequested)
        {
            var current = await WithDbAsync(db => db.Sites.AsNoTracking().Where(s => s.SiteId == siteId).Select(s => s.Version).FirstOrDefaultAsync());
            if (current >= version)
            {
                return;
            }

            await Task.Delay(50, CancellationToken.None);
        }

        throw new TimeoutException($"Site {siteId} did not reach version {version}.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Sync:HeartbeatInterval", HeartbeatInterval.ToString());
}
