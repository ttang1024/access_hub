using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Infrastructure.Consumers;
using AccessHub.Alarm.Infrastructure.Persistence;
using AccessHub.Alarm.Infrastructure.Sagas;
using AccessHub.BuildingBlocks.Infrastructure.Messaging;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.Alarm.Infrastructure;

public static class AlarmInfrastructureRegistration
{
    public const string ConnectionName = "alarmdb";
    public const string ServiceName = "alarm";

    /// <summary>The live feed only pushes to dashboards; it writes no state, so it skips the inbox (ADR 0010).</summary>
    public const string LiveFeedEndpoint = ServiceName + "-live-feed";

    public static IHostApplicationBuilder AddAlarmInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddAccessHubPersistence<AlarmDbContext>(ConnectionName, AlarmDbContext.SchemaName);
        builder.AddAccessHubMessaging<AlarmDbContext>(ServiceName, AddConsumersAndSaga, [LiveFeedEndpoint]);
        builder.Services.AddAlarmStores();
        builder.Services.AddOptions<AlarmOptions>().BindConfiguration(AlarmOptions.Section);
        builder.Services.AddSingleton<AlarmMetrics>();
        builder.Services.AddHostedService<AlarmHousekeeping>();
        return builder;
    }

    /// <summary>Consumers and the saga, shared with tests that run them on the MassTransit test harness.</summary>
    public static void AddConsumersAndSaga(IBusRegistrationConfigurator x)
    {
        x.AddConsumer<AlarmRulesConsumer, AlarmRulesConsumerDefinition>();
        x.AddConsumer<LiveFeedConsumer, LiveFeedConsumerDefinition>();
        x.AddConsumer<AlarmChangesConsumer>();
        x.AddSagaStateMachine<AlarmStateMachine, AlarmState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                r.ExistingDbContext<AlarmDbContext>();
                r.UsePostgres();
            });
    }

    public static IServiceCollection AddAlarmStores(this IServiceCollection services)
    {
        services.AddScoped<IDenialStore, DenialStore>();
        services.AddScoped<IAlarmReadModel, AlarmReadModel>();
        services.AddScoped<IAlarmTriggerSink, AlarmTriggerSink>();
        services.AddScoped<IAlarmAudit, AlarmAudit>();
        services.AddScoped<IAlarmCommandSender, AlarmCommandSender>();
        return services;
    }
}
