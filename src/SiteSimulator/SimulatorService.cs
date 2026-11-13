using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Seeding;
using AccessHub.SiteSimulator.Simulation;
using AccessHub.SiteSimulator.Storage;
using AccessHub.SiteSimulator.Sync;
using AccessHub.SiteSimulator.Uploading;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.SiteSimulator;

/// <summary>Runs N site controllers and, if asked, a simulated outage.</summary>
public sealed partial class SimulatorService(
    CloudSeeder seeder,
    IHttpClientFactory httpClients,
    NetworkConnectivity connectivity,
    SimulatorMetrics metrics,
    IOptions<SimulatorOptions> options,
    TimeProvider clock,
    ILoggerFactory loggerFactory,
    ILogger<SimulatorService> logger) : BackgroundService
{
    public const string GatewayClient = "gateway";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        var sites = await seeder.EnsureSeededAsync(stoppingToken);
        using var channel = GrpcChannel.ForAddress(
            o.GatewayGrpcUrl ?? throw new InvalidOperationException("Simulator:GatewayGrpcUrl is not configured."),
            new GrpcChannelOptions
            {
                // No HttpClientFactory resilience here: its request timeouts would cut the long-lived change stream.
                HttpHandler = new ConnectivityHandler(connectivity, clock)
                {
                    InnerHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(20),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    },
                },
                DisposeHttpClient = true,
            });
        var client = new Proto.AccessListSync.AccessListSyncClient(channel);

        var controllers = sites.Select(site => Create(site, client, o)).ToList();
        LogStarted(logger, controllers.Count);
        var running = controllers.Select(c => c.RunAsync(o.SwipeInterval, stoppingToken)).ToList();
        if (o.SimulateOutage is { } outage)
        {
            running.Add(SimulateOutageAsync(o.OutageDelay, outage, stoppingToken));
        }

        // Fail loudly: if any site loop dies, stop the process (and show it as failed in Aspire) rather than leave a site
        // half-alive, for example deciding and buffering but never uploading.
        while (running.Count > 0)
        {
            var finished = await Task.WhenAny(running);
            running.Remove(finished);
            if (finished.IsFaulted)
            {
                LogLoopFailed(logger, finished.Exception);
                await finished;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "A site controller loop failed; stopping the simulator")]
    private static partial void LogLoopFailed(ILogger logger, Exception exception);

    private SiteController Create(SiteIdentity site, Proto.AccessListSync.AccessListSyncClient client, SimulatorOptions o)
    {
        var store = new SiteStore(Path.Combine(o.DataDirectory, $"site-{site.SiteId}.db"));
        var holder = new AccessListHolder();
        var http = httpClients.CreateClient(GatewayClient);
        http.DefaultRequestHeaders.Add(SiteIdentity.SiteIdHeader, site.SiteId.ToString());
        http.DefaultRequestHeaders.Add(SiteIdentity.ApiKeyHeader, site.ApiKey);
        return new SiteController(
            site,
            store,
            holder,
            new AccessListSyncWorker(site, client, store, holder, connectivity, clock, loggerFactory.CreateLogger<AccessListSyncWorker>()),
            new EventUploader(site, http, store, clock, loggerFactory.CreateLogger<EventUploader>(), o.UploadBatchSize),
            new SwipeGenerator(site, holder, store, metrics, clock, new Random()));
    }

    private async Task SimulateOutageAsync(TimeSpan delay, TimeSpan duration, CancellationToken stoppingToken)
    {
        await Task.Delay(delay, clock, stoppingToken);
        LogOutage(logger, duration.TotalSeconds);
        connectivity.StartOutage(duration, clock);
        await Task.Delay(duration, clock, stoppingToken);
        connectivity.EndOutage();
        LogRecovered(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Running {Count} site controllers")]
    private static partial void LogStarted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Simulating an outage: cloud unreachable for {Seconds} s. Sites keep deciding and buffering.")]
    private static partial void LogOutage(ILogger logger, double seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outage over: reconnecting and uploading buffered events")]
    private static partial void LogRecovered(ILogger logger);
}
