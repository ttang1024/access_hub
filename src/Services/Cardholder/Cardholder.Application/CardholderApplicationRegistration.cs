using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Application.IntegrationEvents;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.Cardholder.Application;

public static class CardholderApplicationRegistration
{
    public static IServiceCollection AddCardholderApplication(this IServiceCollection services)
    {
        services.AddApplicationHandlers(typeof(CardholderApplicationRegistration).Assembly);
        services.AddScoped<IDomainEventHandler<SiteRegisteredDomainEvent>, SiteEventsPublisher>();
        services.AddScoped<IDomainEventHandler<SiteApiKeyRotatedDomainEvent>, SiteEventsPublisher>();
        services.AddScoped<IDomainEventHandler<DoorRegisteredDomainEvent>, DoorEventsPublisher>();
        services.AddScoped<IDomainEventHandler<CardholderChangedDomainEvent>, CardholderEventsPublisher>();
        services.AddScoped<IDomainEventHandler<CardIssuedDomainEvent>, CardEventsPublisher>();
        services.AddScoped<IDomainEventHandler<CardRevokedDomainEvent>, CardEventsPublisher>();
        services.AddScoped<IDomainEventHandler<AccessGroupChangedDomainEvent>, AccessGroupEventsPublisher>();
        return services;
    }
}
