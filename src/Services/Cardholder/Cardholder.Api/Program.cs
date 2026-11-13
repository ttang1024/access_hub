using System.Text.Json.Serialization;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Api.Endpoints;
using AccessHub.Cardholder.Application;
using AccessHub.Cardholder.Infrastructure;
using AccessHub.Cardholder.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAccessHubWebDefaults();
builder.AddAccessHubAuthentication();
builder.Services.AddCardholderApplication();
builder.AddCardholderInfrastructure();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (await app.ApplyMigrationsAsync<CardholderDbContext>(args))
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
app.MapSitesEndpoints();
app.MapDoorsEndpoints();
app.MapCardholdersEndpoints();
app.MapCardsEndpoints();
app.MapAccessGroupsEndpoints();

await app.RunAsync();

/// <summary>Entry point; partial so integration tests can reference it from WebApplicationFactory.</summary>
public partial class Program;
