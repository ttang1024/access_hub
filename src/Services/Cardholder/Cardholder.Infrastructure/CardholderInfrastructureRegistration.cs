using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Infrastructure.Persistence;
using AccessHub.Cardholder.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.Cardholder.Infrastructure;

public static class CardholderInfrastructureRegistration
{
    public const string ConnectionName = "cardholderdb";
    public const string ServiceName = "cardholder";

    public static IHostApplicationBuilder AddCardholderInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddAccessHubPersistence<CardholderDbContext>(ConnectionName, CardholderDbContext.SchemaName);

        // Cardholder publishes only; it consumes nothing.
        builder.AddAccessHubMessaging<CardholderDbContext>(ServiceName, _ => { });

        builder.Services.AddScoped<ISiteRepository, SiteRepository>();
        builder.Services.AddScoped<IDoorRepository, DoorRepository>();
        builder.Services.AddScoped<ICardholderRepository, CardholderRepository>();
        builder.Services.AddScoped<ICardRepository, CardRepository>();
        builder.Services.AddScoped<IAccessGroupRepository, AccessGroupRepository>();
        builder.Services.AddScoped<ICardholderReadModel, CardholderReadModel>();
        return builder;
    }
}
