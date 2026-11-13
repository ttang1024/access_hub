using AccessHub.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.Alarm.Application;

public static class AlarmApplicationRegistration
{
    public static IServiceCollection AddAlarmApplication(this IServiceCollection services) =>
        services.AddApplicationHandlers(typeof(AlarmApplicationRegistration).Assembly);
}
