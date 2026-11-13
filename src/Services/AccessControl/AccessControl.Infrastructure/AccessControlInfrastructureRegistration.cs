using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Infrastructure.Consumers;
using AccessHub.AccessControl.Infrastructure.Persistence;
using AccessHub.AccessControl.Infrastructure.Sync;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.AccessControl.Infrastructure;

public static class AccessControlInfrastructureRegistration
{
    public const string ConnectionName = "accesscontroldb";
    public const string ServiceName = "access-control";

    public static IHostApplicationBuilder AddAccessControlInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddAccessHubPersistence<AccessControlDbContext>(ConnectionName, AccessControlDbContext.SchemaName);
        builder.AddAccessHubMessaging<AccessControlDbContext>(ServiceName, AddConsumers);
        builder.Services.AddAccessControlStores();
        builder.Services.AddSingleton<SiteChangeFeed>();
        builder.Services.AddSingleton<ISiteChangeFeed>(sp => sp.GetRequiredService<SiteChangeFeed>());
        builder.Services.AddSingleton<ISyncStatusTracker, SyncStatusTracker>();
        builder.Services.AddSingleton<ChangeLogPruner>();
        builder.Services.AddHostedService<SiteChangePoller>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ChangeLogPruner>());
        return builder;
    }

    /// <summary>Consumers, shared with the messaging tests that run them on the in-memory test harness.</summary>
    public static void AddConsumers(IBusRegistrationConfigurator x)
    {
        x.AddConsumer<CardholderEventsConsumer>();
        x.AddConsumer<DoorEventsConsumer, DoorEventsConsumerDefinition>();
    }

    public static IServiceCollection AddAccessControlStores(this IServiceCollection services)
    {
        services.AddScoped<IProjectionStore, ProjectionStore>();
        services.AddScoped<ISiteChangeLog, SiteChangeLog>();
        services.AddScoped<IAccessControlReadModel, AccessControlReadModel>();
        services.AddScoped<ISiteApiKeyStore, SiteApiKeyStore>();
        services.AddScoped<IStaleGrantStore, StaleGrantStore>();
        return services;
    }
}
