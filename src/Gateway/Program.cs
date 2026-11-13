using AccessHub.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.UseDedicatedGrpcPort();
builder.AddServiceDefaults();

// REST calls get a default timeout; the gRPC change-stream route opts out (TimeoutPolicy: Disable) because it stays open.
builder.Services.AddRequestTimeouts(o => o.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(30) });

// The gateway owns no data and cannot validate credentials; every service authenticates its own callers.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(GatewayConstants.ReverseProxySection))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddSiteRateLimiting(builder.Configuration);

var origins = builder.Configuration.GetSection(GatewayConstants.CorsOriginsKey).Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy(GatewayConstants.DashboardCorsPolicy, p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseCors(GatewayConstants.DashboardCorsPolicy);
app.UseRequestTimeouts();
app.UseRateLimiter();
app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();

/// <summary>Entry point; partial so tests can reference it from WebApplicationFactory.</summary>
public partial class Program;
