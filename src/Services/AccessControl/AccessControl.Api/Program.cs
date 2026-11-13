using AccessHub.AccessControl.Api.Endpoints;
using AccessHub.AccessControl.Api.Grpc;
using AccessHub.AccessControl.Application;
using AccessHub.AccessControl.Infrastructure;
using AccessHub.AccessControl.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

builder.UseDedicatedGrpcPort();
builder.AddServiceDefaults();
builder.AddAccessHubWebDefaults();
builder.AddAccessHubAuthentication().AddSiteApiKeyAuthentication();
builder.Services.AddAccessControlApplication();
builder.AddAccessControlInfrastructure();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();

var app = builder.Build();

if (await app.ApplyMigrationsAsync<AccessControlDbContext>(args))
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
app.MapGrpcService<AccessListSyncService>();
app.MapAccessControlEndpoints();

await app.RunAsync();

/// <summary>Entry point; partial so integration tests can reference it from WebApplicationFactory.</summary>
public partial class Program;
