using System.Text.Json.Serialization;
using AccessHub.Alarm.Api.Endpoints;
using AccessHub.Alarm.Api.Hubs;
using AccessHub.Alarm.Application;
using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Infrastructure;
using AccessHub.Alarm.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using StackExchange.Redis;

const string RedisConnectionName = "redis";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAccessHubWebDefaults();
builder.AddAccessHubAuthentication();
builder.Services.AddAlarmApplication();
builder.AddAlarmInfrastructure();
builder.Services.AddScoped<IDashboardNotifier, SignalRDashboardNotifier>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Redis is used only as the SignalR backplane, so every replica can reach every connected dashboard.
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString(RedisConnectionName) ?? throw new InvalidOperationException($"Connection string '{RedisConnectionName}' is not configured."),
        o => o.Configuration.ChannelPrefix = RedisChannel.Literal("accesshub"));

var app = builder.Build();

if (await app.ApplyMigrationsAsync<AlarmDbContext>(args))
{
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapDefaultEndpoints();
app.MapAlarmEndpoints();
app.MapHub<AlarmsHub>(AlarmsHub.Route);

await app.RunAsync();

/// <summary>Entry point; partial so integration tests can reference it from WebApplicationFactory.</summary>
public partial class Program;
