using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Infrastructure.Consumers;
using AccessHub.EventIngestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.EventIngestion.Infrastructure;

public static class EventIngestionInfrastructureRegistration
{
    public const string ConnectionName = "ingestiondb";
    public const string ServiceName = "event-ingestion";

    public static IHostApplicationBuilder AddEventIngestionInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddAccessHubPersistence<IngestionDbContext>(ConnectionName, IngestionDbContext.SchemaName);
        builder.AddAccessHubMessaging<IngestionDbContext>(ServiceName, x => x.AddConsumer<SiteKeyConsumer>());
        builder.Services.AddScoped<IDoorEventStore, DoorEventStore>();
        builder.Services.AddScoped<KnownSiteStore>();
        builder.Services.AddScoped<IKnownSiteStore>(sp => sp.GetRequiredService<KnownSiteStore>());
        builder.Services.AddScoped<ISiteApiKeyStore>(sp => sp.GetRequiredService<KnownSiteStore>());
        return builder;
    }
}
