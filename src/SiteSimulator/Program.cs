using System.CommandLine;
using AccessHub.SiteSimulator;
using AccessHub.SiteSimulator.Connectivity;
using AccessHub.SiteSimulator.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var sites = new Option<int>("--sites") { Description = "Number of site controllers to run.", DefaultValueFactory = _ => 2 };
var doors = new Option<int>("--doors") { Description = "Doors per site.", DefaultValueFactory = _ => 4 };
var cardholders = new Option<int>("--cardholders") { Description = "Cardholders (one card each) per site, created on first run.", DefaultValueFactory = _ => 40 };
var swipeMs = new Option<int>("--swipe-interval-ms") { Description = "Average time between swipes at each site.", DefaultValueFactory = _ => 1000 };
var outage = new Option<int?>("--simulate-outage") { Description = "Cut connectivity to the cloud for this many seconds." };
var outageDelay = new Option<int>("--outage-delay") { Description = "Seconds to run normally before the simulated outage.", DefaultValueFactory = _ => 30 };
var dataDir = new Option<string?>("--data-dir") { Description = "Where each site keeps its SQLite database and identity." };

var root = new RootCommand("AccessHub site controller simulator: decides every swipe locally and syncs with the cloud.")
{
    sites, doors, cardholders, swipeMs, outage, outageDelay, dataDir,
};

root.SetAction(async (parse, cancellationToken) =>
{
    var builder = Host.CreateApplicationBuilder();
    builder.AddServiceDefaults();

    builder.Services.AddOptions<SimulatorOptions>()
        .Bind(builder.Configuration.GetSection(SimulatorOptions.Section))
        .PostConfigure(o =>
        {
            o.Sites = parse.GetValue(sites);
            o.DoorsPerSite = parse.GetValue(doors);
            o.CardholdersPerSite = parse.GetValue(cardholders);
            o.SwipeInterval = TimeSpan.FromMilliseconds(parse.GetValue(swipeMs));
            o.OutageDelay = TimeSpan.FromSeconds(parse.GetValue(outageDelay));
            o.SimulateOutage = parse.GetValue(outage) is { } seconds ? TimeSpan.FromSeconds(seconds) : o.SimulateOutage;
            o.DataDirectory = parse.GetValue(dataDir) ?? o.DataDirectory;

            // Aspire service discovery injects the gateway's endpoints.
            o.GatewayUrl ??= Uri.TryCreate(builder.Configuration["services:gateway:http:0"], UriKind.Absolute, out var http) ? http : null;
            o.GatewayGrpcUrl ??= Uri.TryCreate(builder.Configuration["services:gateway:grpc:0"], UriKind.Absolute, out var grpc) ? grpc : null;
        });

    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<NetworkConnectivity>();
    builder.Services.AddSingleton<SimulatorMetrics>();
    builder.Services.AddTransient<ConnectivityHandler>();

    // Event upload is idempotent, so the standard resilience pipeline (retries with jitter) is safe here.
    builder.Services.AddHttpClient(SimulatorService.GatewayClient, (sp, c) => c.BaseAddress = Gateway(sp))
        .AddHttpMessageHandler<ConnectivityHandler>();

    // Seeding POSTs are not idempotent, so these clients sit outside the factory's default retry pipeline;
    // the seeder retries the whole run itself.
    builder.Services.AddSingleton(sp => new TokenClient(new HttpClient(), sp.GetRequiredService<IOptions<SimulatorOptions>>()));
    builder.Services.AddSingleton(sp => new CloudSeeder(
        new HttpClient { BaseAddress = Gateway(sp) },
        sp.GetRequiredService<TokenClient>(),
        sp.GetRequiredService<IOptions<SimulatorOptions>>(),
        sp.GetRequiredService<ILogger<CloudSeeder>>()));
    builder.Services.AddHostedService<SimulatorService>();
    builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(45));

    using var host = builder.Build();
    await host.RunAsync(cancellationToken);
    return 0;
});

return await root.Parse(args).InvokeAsync();

static Uri Gateway(IServiceProvider sp) =>
    sp.GetRequiredService<IOptions<SimulatorOptions>>().Value.GatewayUrl
        ?? throw new InvalidOperationException("Simulator:GatewayUrl is not configured.");
