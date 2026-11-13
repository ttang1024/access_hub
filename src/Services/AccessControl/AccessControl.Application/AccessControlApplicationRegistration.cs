using AccessHub.AccessControl.Application.Projection;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.AccessControl.Application;

public static class AccessControlApplicationRegistration
{
    public static IServiceCollection AddAccessControlApplication(this IServiceCollection services)
    {
        services.AddApplicationHandlers(typeof(AccessControlApplicationRegistration).Assembly);
        services.AddScoped<SiteListReconciler>();
        services.AddScoped<SiteChangeStreamer>();
        services.AddSingleton<AccessControlMetrics>();
        services.AddOptions<SyncOptions>().BindConfiguration(SyncOptions.Section);
        return services;
    }
}
