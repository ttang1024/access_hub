using AccessHub.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.EventIngestion.Application;

public static class EventIngestionApplicationRegistration
{
    public static IServiceCollection AddEventIngestionApplication(this IServiceCollection services)
    {
        services.AddApplicationHandlers(typeof(EventIngestionApplicationRegistration).Assembly);
        services.AddSingleton<IngestionMetrics>();
        return services;
    }
}
