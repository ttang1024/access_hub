using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.BuildingBlocks.Infrastructure.Messaging;

/// <summary>MassTransit setup shared by every service: RabbitMQ, EF Core outbox and inbox, retries, delayed scheduler.</summary>
public static class MessagingRegistration
{
    /// <summary>Name of the RabbitMQ connection string (set by Aspire).</summary>
    public const string ConnectionName = "messaging";

    /// <summary>
    /// Registers MassTransit on RabbitMQ with the transactional outbox for <typeparamref name="TContext"/>.
    /// Queue names are prefixed with <paramref name="serviceName"/> so each service gets its own copy of each event.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="serviceName">Prefix for queue names.</param>
    /// <param name="configureConsumers">Registers the service's consumers and sagas.</param>
    /// <param name="statelessEndpoints">
    /// Endpoints whose consumers write no state (for example a live push to dashboards). They skip the inbox/outbox
    /// transaction; duplicates are harmless for them by design (see ADR 0010).
    /// </param>
    public static IHostApplicationBuilder AddAccessHubMessaging<TContext>(
        this IHostApplicationBuilder builder, string serviceName, Action<IBusRegistrationConfigurator> configureConsumers, IReadOnlyCollection<string>? statelessEndpoints = null)
        where TContext : AccessHubDbContext
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionName}' is not configured.");

        builder.Services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        builder.Services.AddMassTransit(x =>
        {
            x.ConfigureAccessHubBus<TContext>(serviceName, statelessEndpoints);
            configureConsumers(x);
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(connectionString));
                cfg.ConfigureDoorEventTopology();
                cfg.UseDelayedMessageScheduler();
                cfg.ConfigureEndpoints(context);
            });
        });
        builder.Services.AddOptions<MassTransitHostOptions>().Configure(o =>
        {
            o.WaitUntilStarted = false;
            o.StopTimeout = TimeSpan.FromSeconds(30);
        });
        return builder;
    }

    /// <summary>
    /// Transport-independent bus configuration, reused by tests with the in-memory test harness:
    /// EF Core outbox (bus outbox for publishing outside consumers), inbox deduplication on every consumer,
    /// exponential retry then the error queue.
    /// </summary>
    public static void ConfigureAccessHubBus<TContext>(this IBusRegistrationConfigurator x, string serviceName, IReadOnlyCollection<string>? statelessEndpoints = null)
        where TContext : AccessHubDbContext
    {
        x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, includeNamespace: false));
        x.AddDelayedMessageScheduler();
        x.AddEntityFrameworkOutbox<TContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox();

            // Read committed: consumers that take locks (projections) must see data committed while they waited.
            o.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
            o.QueryDelay = TimeSpan.FromMilliseconds(250);
            o.DuplicateDetectionWindow = TimeSpan.FromDays(7);
        });
        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseMessageRetry(r => r.Exponential(6, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(500)));
            if (statelessEndpoints?.Contains(name) != true)
            {
                cfg.UseEntityFrameworkOutbox<TContext>(context);
            }
        });
    }
}
